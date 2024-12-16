using System.Text;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EventStore.Client;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Persistence.Kurrent.ApplicationServices;

/// <summary>
///     Provides an event store to Kurrent
///
///     Important to note that our aggregate versioning is 1-based, while Kurrent's is 0-based. So an aggregate event with version 6 will be stored inside a Kurrent event with EventNumber = 5.
/// </summary>
public class KurrentEventStore : IEventStore
{
    private readonly EventStoreClient _client;
    private readonly IRecorder _recorder;
    private const string ConnectionStringSettingName =
        "ApplicationServices:Persistence:EventStoreDb:ConnectionString";
    
    public static KurrentEventStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        // See https://developers.eventstore.com/clients/grpc/getting-started.html#connection-string
        var connectionString = settings.GetString(ConnectionStringSettingName);
        return new KurrentEventStore(recorder, connectionString);
    }

    private KurrentEventStore(IRecorder recorder, string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);

        _client = new EventStoreClient(settings);
        _recorder = recorder;
    }

    public async Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.KurrentEventStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.KurrentEventStore_MissingEntityId);
        events.ThrowIfNullParameter(nameof(events));
        events.ThrowIfInvalidParameter(e => e.HasAny(), nameof(events),
            Resources.KurrentEventStore_AddEventsAsync_NoEvents);

        var streamName = GetEventStreamName(entityName, entityId);

        var eventData = events.Select(@event => new EventData(
            Uuid.NewUuid(),
            @event.EventType,
            @event.ToEvent(),
            contentType: HttpConstants.ContentTypes.Json
        )).ToArray();

        var firstEventVersion = events.First().Version;

        // Used to detect if someone modified the stream after we read it
        // See https://developers.eventstore.com/clients/grpc/appending-events.html#handling-concurrency 
        var expectedKurrentRevision = firstEventVersion == EventStream.FirstVersion
            ? StreamRevision.None
            : StreamRevision.FromInt64(AggregateVersionToKurrentEventNumber(firstEventVersion) - 1);

        try
        {
            await _client.AppendToStreamAsync(streamName, expectedKurrentRevision, eventData,
                cancellationToken: cancellationToken);
            return streamName;
        }
        catch (WrongExpectedVersionException ex)
        {
            var actualKurrentRevision = ex.ActualStreamRevision;

            if (IsStreamUnexpectedlyEmpty(expectedKurrentRevision, actualKurrentRevision))
            {
                return Error.EntityExists(
                    Common.Resources.EventStore_ConcurrencyVerificationFailed_StreamReset.Format(streamName));
            }

            if (IsStreamAlreadyUpdated(expectedKurrentRevision, actualKurrentRevision))
            {
                return Error.EntityExists(
                    Common.Resources.EventStore_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format(streamName,
                        firstEventVersion));
            }

            if (IsStreamMissingUpdates(expectedKurrentRevision, actualKurrentRevision))
            {
                var missingFromEventVersion = KurrentEventNumberToAggregateVersion(actualKurrentRevision.ToInt64()) + 1;
                var missingToEventVersion = firstEventVersion;

                return Error.EntityExists(
                    Common.Resources.EventStore_ConcurrencyVerificationFailed_MissingUpdates.Format(streamName,
                        missingFromEventVersion, missingToEventVersion));
            }

            return ex.ToError(ErrorCode.EntityExists);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, "KurrentEventStore failed to add events to stream {StreamName}", streamName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private static bool IsStreamMissingUpdates(StreamRevision expectedKurrentRevision,
        StreamRevision actualKurrentRevision)
    {
        var streamMissingUpdates = expectedKurrentRevision != StreamRevision.None
                                   && (actualKurrentRevision == StreamRevision.None
                                       || expectedKurrentRevision > actualKurrentRevision);
        return streamMissingUpdates;
    }

    private static bool IsStreamAlreadyUpdated(StreamRevision expectedKurrentRevision,
        StreamRevision actualKurrentRevision)
    {
        var streamAlreadyCreated = expectedKurrentRevision == StreamRevision.None
                                   && actualKurrentRevision != StreamRevision.None;

        var streamAlreadyUpdated = expectedKurrentRevision != StreamRevision.None
                                   && actualKurrentRevision != StreamRevision.None
                                   && expectedKurrentRevision < actualKurrentRevision;

        return streamAlreadyCreated || streamAlreadyUpdated;
    }

    private static bool IsStreamUnexpectedlyEmpty(StreamRevision expectedKurrentRevision,
        StreamRevision actualKurrentRevision)
    {
        var streamUnexpectedlyEmpty = expectedKurrentRevision != StreamRevision.None
                                      && actualKurrentRevision == StreamRevision.None;
        return streamUnexpectedlyEmpty;
    }

#if TESTINGONLY
    async Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.KurrentEventStore_MissingEntityName);

        try
        {
            var result = _client.ReadAllAsync(Direction.Forwards, Position.Start, cancellationToken: cancellationToken);

            var entityEventStreams = new HashSet<string>();
            await foreach (var resolvedEvent in result)
            {
                if (resolvedEvent.Event.EventStreamId.StartsWith(entityName))
                {
                    entityEventStreams.Add(resolvedEvent.Event.EventStreamId);
                }
            }

            foreach (var streamName in entityEventStreams)
            {
                await _client.TombstoneAsync(streamName, StreamState.Any, cancellationToken: cancellationToken);
            }

            return Result.Ok;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, "KurrentEventStore failed to destroy all events for entity {EntityName}",
                entityName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.KurrentEventStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.KurrentEventStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);
        var events = new List<EventSourcedChangeEvent>();

        try
        {
            var result = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start,
                cancellationToken: cancellationToken);
            await foreach (var resolvedEvent in result)
            {
                var data = resolvedEvent.Event.Data.ToArray();
                var @event = data.FromEvent();
                events.Add(@event);
            }

            return events;
        }
        catch (StreamNotFoundException)
        {
            return new List<EventSourcedChangeEvent>();
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, "KurrentEventStore failed to read events from stream {StreamName}", streamName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private static string GetEventStreamName(string entityName, string entityId)
    {
        return $"{entityName}_{entityId}";
    }

    private static long KurrentEventNumberToAggregateVersion(long kurrentEventNumber)
    {
        return kurrentEventNumber + 1;
    }

    private static long AggregateVersionToKurrentEventNumber(long aggregateVersion)
    {
        return aggregateVersion - 1;
    }
}

public class EventWrapper
{
    public required string Data { get; set; }

    public required string EntityType { get; set; }

    public required string EventId { get; set; }

    public required string EventType { get; set; }

    public required int EventVersion { get; set; }

    public required bool IsTombstone { get; set; }

    public required string Metadata { get; set; }
}

public static class KurrentEventStoreConversionExtensions
{
    private static readonly Encoding JsonEncoding = Encoding.UTF8;

    public static EventSourcedChangeEvent FromEvent(this byte[] bytes)
    {
        var unwrapped = JsonEncoding.GetString(bytes)
            .FromJson<EventWrapper>()!;

        var version = unwrapped.EventVersion;
        var isTombstone = unwrapped.IsTombstone;
        var eventId = unwrapped.EventId.ToId();
        var @event = EventSourcedChangeEvent.Create(eventId, unwrapped.EntityType, isTombstone, unwrapped.EventType,
            unwrapped.Data, unwrapped.Metadata, version);

        return @event;
    }

    public static byte[] ToEvent(this EventSourcedChangeEvent @event)
    {
        var wrapped = new EventWrapper
        {
            Data = @event.Data,
            EntityType = @event.EntityType,
            EventId = @event.Id,
            EventType = @event.EventType,
            EventVersion = @event.Version,
            Metadata = @event.Metadata,
            IsTombstone = @event.IsTombstone
        };

        var json = wrapped.ToJson(false)!;
        return JsonEncoding.GetBytes(json);
    }
}