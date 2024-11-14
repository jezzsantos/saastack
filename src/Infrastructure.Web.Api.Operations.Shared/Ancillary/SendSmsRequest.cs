using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Sends an SMS message for delivery
/// </summary>
[Route("/smses/send", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class SendSmsRequest : UnTenantedRequest<SendSmsRequest, DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}