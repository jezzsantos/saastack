using Application.Interfaces;
using Application.Interfaces.Services;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can notify provisioning events
/// </summary>
public interface IProvisioningNotificationService
{
    /// <summary>
    ///     Notifies of the provisioning event
    /// </summary>
    Task<Result<Error>> NotifyAsync(ICallerContext caller, string tenantId, TenantSettings settings,
        CancellationToken cancellationToken);
}