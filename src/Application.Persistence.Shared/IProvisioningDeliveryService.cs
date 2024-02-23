using Application.Interfaces;
using Application.Interfaces.Services;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can deliver provisioning events
/// </summary>
public interface IProvisioningDeliveryService
{
    /// <summary>
    ///     Delivers the provisioning event
    /// </summary>
    Task<Result<Error>> DeliverAsync(ICallerContext context, string tenantId, TenantSettings settings,
        CancellationToken cancellationToken);
}