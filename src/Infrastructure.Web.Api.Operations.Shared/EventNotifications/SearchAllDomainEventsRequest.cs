#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications;

/// <summary>
///     Lists all available domain events
/// </summary>
[Route("/domain_events", OperationMethod.Search, isTestingOnly: true)]
public class SearchAllDomainEventsRequest : UnTenantedSearchRequest<SearchAllDomainEventsResponse>;
#endif