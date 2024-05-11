using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/provisioning/notify", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class NotifyProvisioningRequest : UnTenantedRequest<DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}