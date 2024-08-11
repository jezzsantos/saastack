using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Delivers an audit message
/// </summary>
[Route("/audits/deliver", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DeliverAuditRequest : UnTenantedRequest<DeliverAuditRequest, DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}