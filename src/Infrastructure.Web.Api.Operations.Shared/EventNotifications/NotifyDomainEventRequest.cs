using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Notifies when a domain_event has been raised
/// </summary>
[Route("/event_notifications", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class NotifyDomainEventRequest : UnTenantedRequest<NotifyDomainEventRequest, DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}