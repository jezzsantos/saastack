using Common;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EndUsersInfrastructure.IntegrationTests.Stubs;

public class StubEventNotificationMessageBroker : IEventNotificationMessageBroker
{
    public IIntegrationEvent? LastPublishedEvent { get; private set; }

    public Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        LastPublishedEvent = integrationEvent;
        return Task.FromResult(Result.Ok);
    }

    public void Reset()
    {
        LastPublishedEvent = null;
    }
}