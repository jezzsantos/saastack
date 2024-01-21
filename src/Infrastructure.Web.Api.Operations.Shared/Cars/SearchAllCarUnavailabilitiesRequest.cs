#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}/unavailabilities", ServiceOperation.Search, isTestingOnly: true)]
public class SearchAllCarUnavailabilitiesRequest : TenantedSearchRequest<SearchAllCarUnavailabilitiesResponse>
{
    public required string Id { get; set; }
}
#endif