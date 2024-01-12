using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can deliver usages events
/// </summary>
public interface IUsageDeliveryService
{
    /// <summary>
    ///     Delivers the usage event
    /// </summary>
    Task<Result<Error>> DeliverAsync(ICallerContext context, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default);
}