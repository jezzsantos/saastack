#if TESTINGONLY
namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

[Route("/cars/{id}/unavailabilities", ServiceOperation.Search, true)]
public class SearchAllCarUnavailabilitiesRequest : TenantedSearchRequest<SearchAllCarUnavailabilitiesResponse>
{
    public required string Id { get; set; }
}
#endif