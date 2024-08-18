using Common;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Azure.ApplicationServices;

partial class AzureSqlServerStore : IEventStore
{
    private static string? _cachedContainerName;

    public async Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.AzureSqlServerStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.AzureSqlServerStore_MissingEntityId);
        ArgumentNullException.ThrowIfNull(events);

        var streamName = DetermineEventStreamName(entityName, entityId);

        var latest = await GetLatestEventAsync(streamName, cancellationToken);
        if (latest.IsFailure)
        {
            return latest.Error;
        }

        var latestStoredEventVersion = latest.Value.HasValue
            ? latest.Value.Value.Version.ToOptional()
            : Optional<int>.None;
        var @checked = this.VerifyConcurrencyCheck(streamName, latestStoredEventVersion, events.First().Version);
        if (@checked.IsFailure)
        {
            return @checked.Error;
        }

        foreach (var @event in events)
        {
            var added = await AddAsync(DetermineEventStoreContainerName(),
                CommandEntity.FromDto(@event.ToTabulated(entityName, streamName)), cancellationToken);
            if (added.IsFailure)
            {
                return added.Error;
            }
        }

        return streamName;
    }

#if TESTINGONLY
    async Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName),
            Resources.AzureSqlServerStore_MissingEntityName);

        return await ExecuteSqlDeleteCommandAsync(DetermineEventStoreContainerName(),
            new KeyValuePair<string, object>(nameof(EventStoreEntity.EntityName), entityName), cancellationToken);
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.AzureSqlServerStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.AzureSqlServerStore_MissingEntityId);

        var streamName = DetermineEventStreamName(entityName, entityId);
        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version);

        var queried = await QueryAsync(DetermineEventStoreContainerName(), query,
            PersistedEntityMetadata.FromType<EventStoreEntity>(), cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value
            .ConvertAll(entity => entity.ToDto<EventSourcedChangeEvent>());
    }

    private async Task<Result<Optional<EventStoreEntity>, Error>> GetLatestEventAsync(string streamName,
        CancellationToken cancellationToken)
    {
        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var metadata = PersistedEntityMetadata.FromType<EventStoreEntity>();

        var queried = await QueryAsync(DetermineEventStoreContainerName(), query, metadata, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var latest = queried.Value.FirstOrDefault();
        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>().ToOptional()
            : Optional<EventStoreEntity>.None;
    }

    private static string DetermineEventStreamName(string entityName, string entityId)
    {
        return $"{entityName}_{entityId}";
    }

    private static string DetermineEventStoreContainerName()
    {
        if (_cachedContainerName.HasNoValue())
        {
            _cachedContainerName = typeof(EventStoreEntity).GetEntityNameSafe();
        }

        return _cachedContainerName;
    }
}