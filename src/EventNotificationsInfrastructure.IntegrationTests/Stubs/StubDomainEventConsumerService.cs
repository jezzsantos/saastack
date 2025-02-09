using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;

namespace EventNotificationsInfrastructure.IntegrationTests.Stubs;

public class StubDomainEventConsumerService : IDomainEventConsumerService
{
    public string? LastEventId { get; private set; }

    public Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        LastEventId = changeEvent.Id;
        return Task.FromResult(Result.Ok);
    }

    public string GetSubscriber()
    {
        return "asubscriberref";
    }

    public void Reset()
    {
        LastEventId = null;
    }
}