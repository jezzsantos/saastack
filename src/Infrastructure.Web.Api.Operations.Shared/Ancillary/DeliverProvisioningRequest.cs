using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/provisioning/deliver", ServiceOperation.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DeliverProvisioningRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}