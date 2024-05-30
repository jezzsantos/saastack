using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using EventNotificationsApplication.Persistence.ReadModels;

namespace EventNotificationsApplication.Persistence;

public interface IDomainEventRepository : IApplicationRepository
{
    Task<Result<Error>> AddAsync(DomainEvent domainEvent, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DomainEvent>, Error>> SearchAllAsync(SearchOptions searchOptions,
        CancellationToken cancellationToken);
}