using Common;

namespace Application.Interfaces.Services;

/// <summary>
///     Defines a service for creating tenant-specific settings
/// </summary>
public interface ITenantSettingsService
{
    Task<Result<TenantSettings, Error>> CreateForTenantAsync(ICallerContext caller, string tenantId,
        CancellationToken cancellationToken);
}