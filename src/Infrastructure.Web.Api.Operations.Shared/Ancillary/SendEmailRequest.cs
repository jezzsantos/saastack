using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Sends an email message for delivery
/// </summary>
[Route("/emails/send", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class SendEmailRequest : UnTenantedRequest<DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}