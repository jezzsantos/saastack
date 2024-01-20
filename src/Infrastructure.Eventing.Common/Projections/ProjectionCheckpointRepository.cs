using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Eventing.Common.Projections;

/// <summary>
///     Provides a repository for checkpoints used in projections
/// </summary>
public sealed class ProjectionCheckpointRepository : IProjectionCheckpointRepository
{
    public const int StartingCheckpointVersion = 1;
    private readonly IDomainFactory _domainFactory;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IDataStore _store;

    public ProjectionCheckpointRepository(IRecorder recorder, IIdentifierFactory idFactory,
        IDomainFactory domainFactory, IDataStore store)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _store = store;
        _domainFactory = domainFactory;
    }

    private static string ContainerName => typeof(Checkpoint).GetEntityNameSafe();

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
#if TESTINGONLY
        return await _store.DestroyAllAsync(ContainerName, cancellationToken);
#else
        await Task.CompletedTask;
        return Result.Ok;
#endif
    }

    public async Task<Result<int, Error>> LoadCheckpointAsync(string streamName, CancellationToken cancellationToken)
    {
        var retrieved = await GetCheckpointAsync(streamName, cancellationToken);
        if (!retrieved.IsSuccessful)
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
        var retrieved = await GetCheckpointAsync(streamName, cancellationToken);
        if (!retrieved.IsSuccessful)
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
            var added = await _store.AddAsync(ContainerName, CommandEntity.FromType(checkpoint.Value),
                cancellationToken);
            if (!added.IsSuccessful)
            {
                return added.Error;
            }
        }
        else
        {
            checkpoint.Value.Position = position;
            var replaced = await _store.ReplaceAsync(ContainerName, checkpoint.Value.Id,
                CommandEntity.FromType(checkpoint.Value),
                cancellationToken);
            if (!replaced.IsSuccessful)
            {
                return replaced.Error;
            }
        }

        _recorder.TraceDebug(null, "Saved checkpoint for {StreamName} to position: {Position}", streamName,
            position);

        return Result.Ok;
    }

    private async Task<Result<Optional<Checkpoint>, Error>> GetCheckpointAsync(string streamName,
        CancellationToken cancellationToken)
    {
        var query = await _store.QueryAsync(ContainerName, Query.From<Checkpoint>()
                .Where<string>(cp => cp.StreamName, ConditionOperator.EqualTo, streamName),
            PersistedEntityMetadata.FromType<Checkpoint>(), cancellationToken);
        if (!query.IsSuccessful)
        {
            return query.Error;
        }

        var checkpoint = query.Value.FirstOrDefault();
        if (checkpoint.NotExists())
        {
            return Optional<Checkpoint>.None;
        }

        return new Result<Optional<Checkpoint>, Error>(checkpoint.ToDomainEntity<Checkpoint>(_domainFactory));
    }
}