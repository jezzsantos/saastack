using Common;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

internal class TestMessageBroker : IEventNotificationMessageBroker
{
    private readonly List<IIntegrationEvent> _projectedEvents = new();

    public IIntegrationEvent[] ProjectedEvents => _projectedEvents.ToArray();

    public Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _projectedEvents.Add(integrationEvent);

        return Task.FromResult(Result.Ok);
    }
}