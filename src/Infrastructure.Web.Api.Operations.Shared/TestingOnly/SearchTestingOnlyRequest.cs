#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests Search APIs
/// </summary>
[Route("/testingonly/search", OperationMethod.Search, isTestingOnly: true)]
public class SearchTestingOnlyRequest : UnTenantedSearchRequest<SearchTestingOnlyRequest, SearchTestingOnlyResponse>
{
}
#endif