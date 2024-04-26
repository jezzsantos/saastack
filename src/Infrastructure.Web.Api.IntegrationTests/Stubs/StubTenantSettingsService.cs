using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Common.ApplicationServices;

namespace Infrastructure.Web.Api.IntegrationTests.Stubs;

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
        //Copy this value from the appsettings.Testing.json file
        return $"./saastack/testing/apis/tenants/{tenantId}".WithoutTrailingSlash();
    }
}