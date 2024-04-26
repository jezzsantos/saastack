#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Defines a file repository on the local machine, that stores each entity as raw JSON.
///     store is located in named folders under the <see cref="_rootPath" />
/// </summary>
public partial class LocalMachineJsonFileStore : IEventStore
{
    private const string EventStoreContainerName = "Events";

    public Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.InProcessInMemDataStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);

        var latestStoredEvent = GetLatestEvent(entityName, entityId, streamName);
        var latestStoredEventVersion = latestStoredEvent.HasValue
            ? latestStoredEvent.Value.Version.ToOptional()
            : Optional<int>.None;
        var concurrencyCheck =
            this.VerifyConcurrencyCheck(streamName, latestStoredEventVersion, Enumerable.First(events).Version);
        if (concurrencyCheck.IsFailure)
        {
            return Task.FromResult<Result<string, Error>>(concurrencyCheck.Error);
        }

        events.ForEach(@event =>
        {
            var entity = CommandEntity.FromDto(@event.ToTabulated(entityName, streamName));

            var container = EnsureContainer(GetEventStoreContainerPath(entityName, entityId));
            container.Write(entity.Id, entity.ToFileProperties());
        });

        return Task.FromResult<Result<string, Error>>(streamName);
    }

    Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);

        var eventStore = EnsureContainer(GetEventStoreContainerPath(entityName));
        eventStore.Erase();

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.InProcessInMemDataStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version);

        //HACK: we use QueryEntity.ToDto() here, since EventSourcedChangeEvent can be rehydrated without a IDomainFactory 
        var events = QueryEventStores(entityName, entityId, query)
            .ConvertAll(entity => entity.ToDto<EventSourcedChangeEvent>());

        return Task.FromResult<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>>(events);
    }

    private static string GetEventStoreContainerPath(string containerName, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{EventStoreContainerName}/{containerName}/{entityId}";
        }

        return $"{EventStoreContainerName}/{containerName}";
    }

    private Optional<EventStoreEntity> GetLatestEvent(string entityName, string entityId, string streamName)
    {
        entityId.ThrowIfNotValuedParameter(nameof(entityId));
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var latest = QueryEventStores(entityName, entityId, query)
            .FirstOrDefault();

        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>()
            : Optional<EventStoreEntity>.None;
    }

    private List<QueryEntity> QueryEventStores<TQueryableEntity>(string entityName, string entityId,
        QueryClause<TQueryableEntity> query)
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
        var results = query.FetchAllIntoMemory(MaxQueryResults, metadata,
            () => QueryPrimaryEntities(container, metadata),
            _ => new Dictionary<string, HydrationProperties>());

        return results;
    }

    private static string GetEventStreamName(string entityName, string entityId)
    {
        return $"{entityName}_{entityId}";
    }
}
#endif