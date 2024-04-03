using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a consumer that handles all events and does nothing with them
/// </summary>
public sealed class NoOpDomainEventConsumer : IDomainEventNotificationConsumer
{
    public Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
}