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

public partial class InProcessInMemStore : IEventStore
{
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _events = new();

    public Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);
        entityId.ThrowIfNotValuedParameter(nameof(entityId), Resources.InProcessInMemDataStore_MissingEntityId);

        var streamName = GetEventStreamName(entityName, entityId);

        var latestStoredEvent = GetLatestEvent(entityName, streamName);
        var latestStoredEventVersion = latestStoredEvent.HasValue
            ? latestStoredEvent.Value.Version.ToOptional()
            : Optional<int>.None;
        var concurrencyCheck =
            this.VerifyConcurrencyCheck(streamName, latestStoredEventVersion, Enumerable.First(events).Version);
        if (!concurrencyCheck.IsSuccessful)
        {
            return Task.FromResult<Result<string, Error>>(concurrencyCheck.Error);
        }

        events.ForEach(@event =>
        {
            var entity = CommandEntity.FromDto(@event.ToTabulated(entityName, streamName));
            if (!_events.ContainsKey(entityName))
            {
                _events.Add(entityName, new Dictionary<string, HydrationProperties>());
            }

            _events[entityName].Add(entity.Id, entity.ToHydrationProperties());
        });

        return Task.FromResult<Result<string, Error>>(streamName);
    }

    Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);

        if (_events.ContainsKey(entityName))
        {
            _events.Remove(entityName);
        }

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
        var events = QueryEventStores(entityName, query)
            .ConvertAll(entity => entity.ToDto<EventSourcedChangeEvent>());

        return Task.FromResult<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>>(events);
    }

    private List<QueryEntity> QueryEventStores<TQueryableEntity>(string entityName,
        QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        entityName.ThrowIfNotValuedParameter(nameof(entityName), Resources.InProcessInMemDataStore_MissingEntityName);

        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        if (!_events.ContainsKey(entityName))
        {
            return new List<QueryEntity>();
        }

        var metadata = PersistedEntityMetadata.FromType<EventStoreEntity>();
        var results = query.FetchAllIntoMemory(MaxQueryResults, metadata,
            () => _events[entityName],
            _ => new Dictionary<string, HydrationProperties>());

        return results;
    }

    private static string GetEventStreamName(string entityName, string entityId)
    {
        return $"{entityName}_{entityId}";
    }

    private Optional<EventStoreEntity> GetLatestEvent(string entityName, string streamName)
    {
        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var latest = QueryEventStores(entityName, query)
            .FirstOrDefault();
        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>()
            : Optional<EventStoreEntity>.None;
    }
}
#endif