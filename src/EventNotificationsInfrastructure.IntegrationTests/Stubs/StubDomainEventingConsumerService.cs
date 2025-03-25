using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;

namespace EventNotificationsInfrastructure.IntegrationTests.Stubs;

public class StubDomainEventingConsumerService : IDomainEventingConsumerService
{
    public string? LastEventId { get; private set; }

    public string? LastEventSubscriptionName { get; private set; }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        LastEventId = changeEvent.Id;
        LastEventSubscriptionName = subscriptionName;
        return Result.Ok;
    }

    public IReadOnlyList<string> SubscriptionNames => [];

    public void Reset()
    {
        LastEventId = null;
        LastEventSubscriptionName = null;
    }
}