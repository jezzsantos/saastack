using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/provisioning/notify", ServiceOperation.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class NotifyProvisioningRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}