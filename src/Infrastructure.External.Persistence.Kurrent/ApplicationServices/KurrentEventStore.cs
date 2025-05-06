using System.Text;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EventStore.Client;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Interfaces;

namespace Infrastructure.External.Persistence.Kurrent.ApplicationServices;

/// <summary>
///     Provides an event store to Kurrent
///     Note: Domain events are 1-based, while Kurrent's events are 0-based.
///     e.g. an aggregate domain event at version == 6 will be stored inside a Kurrent event with EventNumber == 5.
///     <see href="https://developers.eventstore.com/clients/grpc/getting-started.html" />
/// </summary>
public class KurrentEventStore : IEventStore
{
    private const string ConnectionStringSettingName = "ApplicationServices:Persistence:Kurrent:ConnectionString";
    private readonly EventStoreClient _client;
    private readonly IRecorder _recorder;

    public static KurrentEventStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        var connectionString = settings.GetString(ConnectionStringSettingName);
        return Create(recorder, connectionString);
    }

    public static KurrentEventStore Create(IRecorder recorder, string connectionString)
    {
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

        // See https://developers.eventstore.com/clients/grpc/appending-events.html#handling-concurrency 
        var firstEventVersion = events.First().Version;
        var lastEventVersion = events.Last().Version;
        var expectedKurrentRevision = firstEventVersion == EventStream.FirstVersion
            ? StreamRevision.None
            : StreamRevision.FromInt64(AggregateVersionToKurrentEventNumber(firstEventVersion) - 1);

        try
        {
            await _client.AppendToStreamAsync(streamName, expectedKurrentRevision, eventData,
                cancellationToken: cancellationToken);

            if (events.Count > 1)
            {
                _recorder.TraceInformation(null,
                    "KurrentEventStore added {Count} events to stream {StreamName}, from version {FromVersion} to version {ToVersion}",
                    events.Count, streamName, firstEventVersion, lastEventVersion);
            }
            else
            {
                _recorder.TraceInformation(null,
                    "KurrentEventStore added 1 event to stream {StreamName}, at version {FromVersion}", streamName,
                    firstEventVersion);
            }

            return streamName;
        }
        catch (WrongExpectedVersionException ex)
        {
            var actualKurrentRevision = ex.ActualStreamRevision;
            var storeType = GetType().Name;

            if (IsStreamUnexpectedlyEmpty(expectedKurrentRevision, actualKurrentRevision))
            {
                return Error.EntityExists(Infrastructure.Persistence.Common.Resources
                    .EventStore_ConcurrencyVerificationFailed_StreamReset.Format(storeType, streamName));
            }

            if (IsStreamAlreadyUpdated(expectedKurrentRevision, actualKurrentRevision))
            {
                return Error.EntityExists(
                    Infrastructure.Persistence.Common.Resources
                        .EventStore_ConcurrencyVerificationFailed_StreamAlreadyUpdated
                        .Format(storeType, streamName, firstEventVersion));
            }

            if (IsStreamMissingUpdates(expectedKurrentRevision, actualKurrentRevision))
            {
                var missingFromEventVersion = KurrentEventNumberToAggregateVersion(actualKurrentRevision.ToInt64()) + 1;
                var missingToEventVersion = firstEventVersion;

                return Error.EntityExists(
                    Infrastructure.Persistence.Common.Resources.EventStore_ConcurrencyVerificationFailed_MissingUpdates
                        .Format(storeType, streamName, missingFromEventVersion, missingToEventVersion));
            }

            return ex.ToError(ErrorCode.EntityExists);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                "KurrentEventStore failed to add {Count} events to stream {StreamName}, from version {FromVersion} to version {ToVersion}",
                events.Count, streamName, firstEventVersion, lastEventVersion);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

#if TESTINGONLY
    async Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.KurrentEventStore_MissingEntityName);

        try
        {
            var allStreams =
                _client.ReadAllAsync(Direction.Forwards, Position.Start, cancellationToken: cancellationToken);

            var streamNames = new HashSet<string>();
            await foreach (var resolvedEvent in allStreams)
            {
                if (resolvedEvent.Event.EventStreamId.StartsWith(entityName))
                {
                    streamNames.Add(resolvedEvent.Event.EventStreamId);
                }
            }

            foreach (var streamName in streamNames)
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
            var eventStream = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start,
                cancellationToken: cancellationToken);
            await foreach (var resolvedEvent in eventStream)
            {
                var data = resolvedEvent.Event.Data.ToArray();
                events.Add(data.FromEvent());
            }

            var firstEventVersion = events.First().Version;
            var lastEventVersion = events.Last().Version;
            _recorder.TraceInformation(null,
                "KurrentEventStore retrieved {Count} events from stream {StreamName}, from version {FromVersion} to version {ToVersion}",
                events.Count, streamName, firstEventVersion, lastEventVersion);

            return events;
        }
        catch (StreamNotFoundException)
        {
            return new List<EventSourcedChangeEvent>();
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, "KurrentEventStore failed to read all events from stream {StreamName}",
                streamName);
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