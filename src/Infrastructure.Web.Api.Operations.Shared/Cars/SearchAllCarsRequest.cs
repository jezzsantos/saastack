using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars", ServiceOperation.Search)]
public class SearchAllCarsRequest : TenantedSearchRequest<SearchAllCarsResponse>
{
}