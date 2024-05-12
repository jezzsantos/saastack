using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Notifies when provisioning of a service has been completed
/// </summary>
[Route("/provisioning/notify", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class NotifyProvisioningRequest : UnTenantedRequest<DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}