using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Delivers an email message
/// </summary>
[Route("/emails/deliver", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DeliverEmailRequest : UnTenantedRequest<DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}