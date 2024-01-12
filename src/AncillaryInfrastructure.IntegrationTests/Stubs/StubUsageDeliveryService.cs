using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubUsageDeliveryService : IUsageDeliveryService
{
    public List<string> AllEventNames { get; private set; } = new();

    public Optional<string> LastEventName { get; private set; } = Optional<string>.None;

    public Task<Result<Error>> DeliverAsync(ICallerContext context, string forId, string eventName,
        Dictionary<string, string>? additional = null,
        CancellationToken cancellationToken = default)
    {
        AllEventNames.Add(eventName);
        LastEventName = Optional<string>.Some(eventName);

        return Task.FromResult(Result.Ok);
    }

    public void Reset()
    {
        AllEventNames = new List<string>();
        LastEventName = Optional<string>.None;
    }
}