using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

internal class TestConsumerRelay : IDomainEventConsumerRelay
{
    private readonly List<IDomainEvent> _notifiedEvents = new();

    public IDomainEvent[] NotifiedEvents => _notifiedEvents.ToArray();

    public Task<Result<Error>> RelayDomainEventAsync(IDomainEvent @event,
        EventStreamChangeEvent changeEvent, 
        CancellationToken cancellationToken)
    {
        _notifiedEvents.Add(@event);

        return Task.FromResult(Result.Ok);
    }
}