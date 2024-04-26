using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Projections;

namespace Infrastructure.Eventing.Common.Projections;

/// <summary>
///     Provides a projector of change events to registered read models
/// </summary>
public sealed class ReadModelProjector : IReadModelProjector, IDisposable
{
    private readonly IProjectionCheckpointRepository _checkpointStore;
    private readonly IEventSourcedChangeEventMigrator _migrator;

    // ReSharper disable once NotAccessedField.Local
    private readonly IRecorder _recorder;

    public ReadModelProjector(IRecorder recorder, IProjectionCheckpointRepository checkpointStore,
        IEventSourcedChangeEventMigrator migrator, params IReadModelProjection[] projections)
    {
        _recorder = recorder;
        _checkpointStore = checkpointStore;
        Projections = projections;
        _migrator = migrator;
    }

    public void Dispose()
    {
        if (Projections.Exists())
        {
            foreach (var projection in Projections)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (projection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    public IReadOnlyList<IReadModelProjection> Projections { get; }

    public async Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> eventStream,
        CancellationToken cancellationToken)
    {
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        if (!eventStream.Any())
        {
            return Result.Ok;
        }

        var streamEntityType = Enumerable.First(eventStream).RootAggregateType;
        var firstEventVersion = Enumerable.First(eventStream).Version;
        var projection = GetProjectionForStream(Projections, streamEntityType);
        if (projection.IsFailure)
        {
            return projection.Error;
        }

        var retrieved = await _checkpointStore.LoadCheckpointAsync(streamName, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var checkpointVersion = retrieved.Value;
        var versioned = EnsureNextVersion(streamName, checkpointVersion, firstEventVersion);
        if (versioned.IsFailure)
        {
            return versioned.Error;
        }

        var processed = 0;
        foreach (var changeEvent in SkipPreviouslyProjectedVersions(eventStream, checkpointVersion))
        {
            var deserialized = DeserializeEvent(changeEvent, _migrator);
            if (deserialized.IsFailure)
            {
                return deserialized.Error;
            }

            var projected =
                await ProjectEventAsync(projection.Value, deserialized.Value, changeEvent, cancellationToken);
            if (projected.IsFailure)
            {
                return projected.Error;
            }

            processed++;
        }

        var newCheckpoint = checkpointVersion + processed;
        var saved = await _checkpointStore.SaveCheckpointAsync(streamName, newCheckpoint, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private static async Task<Result<Error>> ProjectEventAsync(IReadModelProjection projection, IDomainEvent @event,
        EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var projected = await projection.ProjectEventAsync(@event, cancellationToken);
        if (projected.IsFailure)
        {
            return projected.Error.Wrap(ErrorCode.Unexpected,
                Resources.ReadModelProjector_ProjectionError_HandlerError.Format(
                    projection.GetType().Name,
                    changeEvent.Id, changeEvent.Metadata.Fqn));
        }

#if TESTINGONLY
        if (!projected.Value)
        {
            //Note: this is for local development and testing only to ensure all events are configured
            return Error.Unexpected(Resources.ReadModelProjector_ProjectionError_MissingHandler.Format(
                projection.GetType().Name,
                changeEvent.Id, changeEvent.Metadata.Fqn));
        }
#endif

        return Result.Ok;
    }

    private static IEnumerable<EventStreamChangeEvent> SkipPreviouslyProjectedVersions(
        IEnumerable<EventStreamChangeEvent> eventStream, int checkpoint)
    {
        return eventStream
            .Where(e => e.Version >= checkpoint);
    }

    private static Result<IReadModelProjection, Error> GetProjectionForStream(
        IEnumerable<IReadModelProjection> projections,
        string entityTypeName)
    {
        var projection = projections.FirstOrDefault(prj => prj.RootAggregateType.Name == entityTypeName);
        if (projection.NotExists())
        {
            return Error.RuleViolation(Resources.ReadModelProjector_ProjectionNotConfigured.Format(entityTypeName));
        }

        return new Result<IReadModelProjection, Error>(projection);
    }

    private static Result<IDomainEvent, Error> DeserializeEvent(EventStreamChangeEvent changeEvent,
        IEventSourcedChangeEventMigrator migrator)
    {
        return changeEvent.Metadata.CreateEventFromJson(changeEvent.Id, changeEvent.Data, migrator);
    }

    private static Result<Error> EnsureNextVersion(string streamName, int checkpointVersion, int firstEventVersion)
    {
        if (firstEventVersion > checkpointVersion)
        {
            return Error.RuleViolation(
                Resources.ReadModelProjector_CheckpointError.Format(streamName, checkpointVersion, firstEventVersion));
        }

        return Result.Ok;
    }
}