using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

[Route("/organizations/{Id}/avatar", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class ChangeOrganizationAvatarRequest : UnTenantedRequest<GetOrganizationResponse>,
    IUnTenantedOrganizationRequest, IHasMultipartForm
{
    // Will also include bytes for the multipart-form image
    public string? Id { get; set; }
}