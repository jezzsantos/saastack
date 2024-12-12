using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Eventing.Common.Projections;

/// <summary>
///     Provides a repository for checkpoints used in projections
/// </summary>
public sealed class ProjectionCheckpointRepository : IProjectionCheckpointRepository
{
    public const int StartingCheckpointVersion = 1;
    private readonly ISnapshottingStore<Checkpoint> _checkpoints;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;

    public ProjectionCheckpointRepository(IRecorder recorder, IIdentifierFactory idFactory, IDataStore store) : this(
        recorder, idFactory, new SnapshottingStore<Checkpoint>(recorder, store))
    {
    }

    internal ProjectionCheckpointRepository(IRecorder recorder, IIdentifierFactory idFactory,
        ISnapshottingStore<Checkpoint> checkpoints)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _checkpoints = checkpoints;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
#if TESTINGONLY
        return await _checkpoints.DestroyAllAsync(cancellationToken);
#else
        await Task.CompletedTask;
        return Result.Ok;
#endif
    }

    public async Task<Result<int, Error>> LoadCheckpointAsync(string streamName, CancellationToken cancellationToken)
    {
        var retrieved = await FindCheckpointByStreamNameAsync(streamName, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var checkpoint = retrieved.Value;
        return checkpoint.HasValue
            ? checkpoint.Value.Position.Value
            : StartingCheckpointVersion;
    }

    public async Task<Result<Error>> SaveCheckpointAsync(string streamName, int position,
        CancellationToken cancellationToken)
    {
        var retrieved = await FindCheckpointByStreamNameAsync(streamName, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var checkpoint = retrieved.Value;
        if (!checkpoint.HasValue)
        {
            checkpoint = new Checkpoint
            {
                Position = position,
                StreamName = streamName
            };
            checkpoint.Value.Id = _idFactory.Create(checkpoint.Value).Value.Text;
            var added = await _checkpoints.UpsertAsync(checkpoint, false, cancellationToken);
            if (added.IsFailure)
            {
                return added.Error;
            }
        }
        else
        {
            checkpoint.Value.Position = position;
            var replaced = await _checkpoints.UpsertAsync(checkpoint.Value, false, cancellationToken);
            if (replaced.IsFailure)
            {
                return replaced.Error;
            }
        }

        _recorder.TraceDebug(null, "Saved checkpoint for {StreamName} to position: {Position}", streamName,
            position);

        return Result.Ok;
    }

    private async Task<Result<Optional<Checkpoint>, Error>> FindCheckpointByStreamNameAsync(string streamName,
        CancellationToken cancellationToken)
    {
        var query = Query.From<Checkpoint>()
            .Where<string>(cp => cp.StreamName, ConditionOperator.EqualTo, streamName);
        var queried = await _checkpoints.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<Checkpoint>.None;
        }

        return matching.ToOptional();
    }
}