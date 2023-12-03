using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

internal class TestConsumer : IEventNotificationConsumer
{
    private readonly List<IDomainEvent> _projectedEvents = new();

    public IDomainEvent[] ProjectedEvents => _projectedEvents.ToArray();

    public Task<Result<bool, Error>> NotifyAsync(IDomainEvent changeEvent, CancellationToken cancellationToken)
    {
        _projectedEvents.Add(changeEvent);

        return Task.FromResult<Result<bool, Error>>(true);
    }
}