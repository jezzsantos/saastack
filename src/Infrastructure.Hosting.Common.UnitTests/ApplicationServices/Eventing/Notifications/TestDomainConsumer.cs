using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

internal class TestDomainConsumer : IDomainEventNotificationConsumer
{
    private readonly List<IDomainEvent> _projectedEvents = new();

    public IDomainEvent[] ProjectedEvents => _projectedEvents.ToArray();

    public Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _projectedEvents.Add(domainEvent);

        return Task.FromResult(Result.Ok);
    }
}