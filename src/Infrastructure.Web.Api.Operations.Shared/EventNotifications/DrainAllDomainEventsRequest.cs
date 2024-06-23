#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Drains all the pending domain_event messages
/// </summary>
[Route("/domain_events/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllDomainEventsRequest : UnTenantedEmptyRequest;
#endif