#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Drains all the pending event notifications
/// </summary>
[Route("/event_notifications/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllEventNotificationsRequest : UnTenantedEmptyRequest<DrainAllEventNotificationsRequest>;
#endif