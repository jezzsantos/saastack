using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces;
using EventNotificationsApplication.Persistence;
using EventNotificationsApplication.Persistence.ReadModels;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace EventNotificationsInfrastructure.Persistence;

public class DomainEventRepository : IDomainEventRepository
{
    private readonly IReadModelStore<DomainEvent> _events;

    public DomainEventRepository(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _events = new ReadModelStore<DomainEvent>(recorder, domainFactory, store);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await _events.DestroyAllAsync(cancellationToken);
    }
#endif

    public async Task<Result<IReadOnlyList<DomainEvent>, Error>> SearchAllAsync(SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        var queried = await _events.QueryAsync(Query.From<DomainEvent>()
            .WhereAll()
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value.Results;
    }

    public async Task<Result<Error>> AddAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var added = await _events.UpsertAsync(domainEvent, false, cancellationToken);
        return added.IsFailure
            ? added.Error
            : Result.Ok;
    }
}