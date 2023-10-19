using Application.Interfaces.Resources;

namespace Application.Interfaces.Services;

/// <summary>
///     Defines an application service for working with tenant-specific settings
/// </summary>
public interface ITenantSettingsService
{
    IReadOnlyDictionary<string, TenantSetting> CreateForNewTenant(ICallerContext context, string tenantId);
}