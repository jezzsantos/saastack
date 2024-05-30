using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Notifies when a domain event has been raised
/// </summary>
[Route("/domain_events/notify", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class NotifyDomainEventRequest : UnTenantedRequest<DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}