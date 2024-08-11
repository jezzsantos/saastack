using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Delivers a usage event
/// </summary>
[Route("/usages/deliver", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DeliverUsageRequest : UnTenantedRequest<DeliverUsageRequest, DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}