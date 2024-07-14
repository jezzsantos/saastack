using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IUsageDeliveryService" />
/// </summary>
public sealed class StubUsageDeliveryService : IUsageDeliveryService
{
    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return Result.Ok;
    }
}