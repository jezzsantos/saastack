using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;

namespace AncillaryInfrastructure.ApplicationServices;

public class OrganizationProvisioningNotificationService : IProvisioningNotificationService
{
    private readonly IOrganizationsService _organizationsService;

    public OrganizationProvisioningNotificationService(IOrganizationsService organizationsService)
    {
        _organizationsService = organizationsService;
    }

    public async Task<Result<Error>> NotifyAsync(ICallerContext caller, string tenantId,
        TenantSettings settings, CancellationToken cancellationToken)
    {
        return await _organizationsService.ChangeSettingsPrivateAsync(caller, tenantId, settings, cancellationToken);
    }
}