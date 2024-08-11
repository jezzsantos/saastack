#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all available audits
/// </summary>
[Route("/audits", OperationMethod.Search, isTestingOnly: true)]
public class SearchAllAuditsRequest : TenantedSearchRequest<SearchAllAuditsRequest, SearchAllAuditsResponse>;
#endif