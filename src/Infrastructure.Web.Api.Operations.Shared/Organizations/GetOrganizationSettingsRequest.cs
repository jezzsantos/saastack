#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/settings", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class GetOrganizationSettingsRequest : UnTenantedRequest<GetOrganizationSettingsResponse>
{
    public required string Id { get; set; }
}
#endif