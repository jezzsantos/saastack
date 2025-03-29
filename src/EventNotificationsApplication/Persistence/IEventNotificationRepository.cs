using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using EventNotificationsApplication.Persistence.ReadModels;

namespace EventNotificationsApplication.Persistence;

public interface IEventNotificationRepository : IApplicationRepository
{
    Task<Result<Error>> SaveAsync(EventNotification notification, CancellationToken cancellationToken);

    Task<Result<QueryResults<EventNotification>, Error>> SearchAllAsync(SearchOptions searchOptions,
        CancellationToken cancellationToken);
}