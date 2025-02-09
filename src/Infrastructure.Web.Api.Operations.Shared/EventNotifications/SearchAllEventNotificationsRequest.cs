#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Lists all event notifications
/// </summary>
[Route("/event_notifications", OperationMethod.Search, isTestingOnly: true)]
public class
    SearchAllEventNotificationsRequest : UnTenantedSearchRequest<SearchAllEventNotificationsRequest,
    SearchAllEventNotificationsResponse>;
#endif