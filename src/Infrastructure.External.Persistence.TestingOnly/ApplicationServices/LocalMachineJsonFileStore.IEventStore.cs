#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

partial class LocalMachineJsonFileStore : IEventStore
{
    private static string? _cachedContainerName;

    public async Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.InProcessInMemDataStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);

        var latestStoredEvent = await GetLatestEventAsync(entityName, entityId, streamName, cancellationToken);
        var latestStoredEventVersion = latestStoredEvent.HasValue
            ? latestStoredEvent.Value.Version.ToOptional()
            : Optional<int>.None;
        var @checked =
            this.VerifyContiguousCheck(streamName, latestStoredEventVersion, Enumerable.First(events).Version);
        if (@checked.IsFailure)
        {
            return @checked.Error;
        }

        foreach (var @event in events)
        {
            var entity = CommandEntity.FromDto(@event.ToTabulated(entityName, streamName));

            var container = EnsureContainer(GetEventStoreContainerPath(entityName, entityId));
            var version = @event.Version;
            var filename = $"version_{version:D3}";
            var added = await container.WriteExclusiveAsync(filename, entity.ToFileProperties(), cancellationToken);
            if (added.IsFailure)
            {
                if (added.Error.Is(ErrorCode.EntityExists))
                {
                    return Error.EntityExists(
                        Infrastructure.Persistence.Common.Resources
                            .EventStore_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format(
                                streamName, version));
                }

                return added.Error;
            }
        }

        return streamName;
    }

#if TESTINGONLY
    Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);

        var eventStore = EnsureContainer(GetEventStoreContainerPath(entityName));
        eventStore.Erase();

        return Task.FromResult(Result.Ok);
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.InProcessInMemDataStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version);

        //HACK: we use QueryEntity.ToDto() here, since EventSourcedChangeEvent can be rehydrated without a IDomainFactory 
        var queries = await QueryEventStoresAsync(entityName, entityId, query, cancellationToken);
        var events = queries
            .ConvertAll(entity => entity.ToDto<EventSourcedChangeEvent>());

        return events;
    }

    private static string GetEventStoreContainerPath(string containerName, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{DetermineEventStoreContainerName()}/{containerName}/{entityId}";
        }

        return $"{DetermineEventStoreContainerName()}/{containerName}";
    }

    private async Task<Optional<EventStoreEntity>> GetLatestEventAsync(string entityName, string entityId,
        string streamName, CancellationToken cancellationToken)
    {
        entityId.ThrowIfNotValuedParameter(nameof(entityId));
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var queries = await QueryEventStoresAsync(entityName, entityId, query, cancellationToken);
        var latest = queries
            .FirstOrDefault();

        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>()
            : Optional<EventStoreEntity>.None;
    }

    private async Task<List<QueryEntity>> QueryEventStoresAsync<TQueryableEntity>(string entityName, string entityId,
        QueryClause<TQueryableEntity> query, CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity
    {
        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        var container = EnsureContainer(GetEventStoreContainerPath(entityName, entityId));
        if (container.IsEmpty())
        {
            return new List<QueryEntity>();
        }

        var metadata = PersistedEntityMetadata.FromType<EventStoreEntity>();
        var results = await query.FetchAllIntoMemoryAsync(MaxQueryResults, metadata,
            () => QueryPrimaryEntitiesAsync(container, metadata, cancellationToken),
            _ => Task.FromResult(new Dictionary<string, HydrationProperties>()));

        return results.Results;
    }

    private static string GetEventStreamName(string entityName, string entityId)
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
#endif