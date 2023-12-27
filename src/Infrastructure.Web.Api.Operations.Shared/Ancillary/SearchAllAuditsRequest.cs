#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/audits", ServiceOperation.Search, isTestingOnly: true)]
public class SearchAllAuditsRequest : TenantedSearchRequest<SearchAllAuditsResponse>
{
}
#endif