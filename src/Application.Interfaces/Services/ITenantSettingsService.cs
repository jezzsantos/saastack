using Common;

namespace Application.Interfaces.Services;

/// <summary>
///     Defines a service for creating tenant-specific settings
/// </summary>
public interface ITenantSettingsService
{
    Task<Result<TenantSettings, Error>> CreateForTenantAsync(ICallerContext context, string tenantId,
        CancellationToken cancellationToken);
}