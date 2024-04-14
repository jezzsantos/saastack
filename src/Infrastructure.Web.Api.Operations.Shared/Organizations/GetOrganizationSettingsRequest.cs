#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/settings", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class GetOrganizationSettingsRequest : UnTenantedRequest<GetOrganizationSettingsResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}
#endif