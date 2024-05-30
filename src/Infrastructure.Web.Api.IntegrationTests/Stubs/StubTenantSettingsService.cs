using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Common.ApplicationServices;

namespace Infrastructure.Web.Api.IntegrationTests.Stubs;

/// <summary>
///     Writes tenant's data to the: ./saastack/testing/apis/tenants/{tenantId} folder on disk
/// </summary>
public class StubTenantSettingsService : ITenantSettingsService
{
    public async Task<Result<TenantSettings, Error>> CreateForTenantAsync(ICallerContext caller, string tenantId,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new TenantSettings(new Dictionary<string, TenantSetting>
        {
#if TESTINGONLY
            {
                LocalMachineJsonFileStore.PathSettingName,
                new TenantSetting { Value = GetRepositoryPath(tenantId) }
            }
#endif
        });
    }

    public static string GetRepositoryPath(string? tenantId)
    {
        // Match this value with the one in the appsettings.Testing.json file
        var path = "./saastack/testing/apis";
        return tenantId.HasValue()
            ? $"{path}/tenants/{tenantId}".WithoutTrailingSlash()
            : path;
    }
}