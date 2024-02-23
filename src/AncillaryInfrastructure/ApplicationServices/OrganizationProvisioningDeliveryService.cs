using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;

namespace AncillaryInfrastructure.ApplicationServices;

public class OrganizationProvisioningDeliveryService : IProvisioningDeliveryService
{
    private readonly IOrganizationsService _organizationsService;

    public OrganizationProvisioningDeliveryService(IOrganizationsService organizationsService)
    {
        _organizationsService = organizationsService;
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext context, string tenantId,
        TenantSettings settings, CancellationToken cancellationToken)
    {
        return await _organizationsService.ChangeSettingsPrivateAsync(context, tenantId, settings, cancellationToken);
    }
}