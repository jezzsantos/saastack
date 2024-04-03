using Common;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides an implementation of <see cref="IEventNotificationMessageBroker" /> that does nothing.
/// </summary>
public class NoOpEventNotificationMessageBroker : IEventNotificationMessageBroker
{
    public async Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Result.Ok;
    }
}