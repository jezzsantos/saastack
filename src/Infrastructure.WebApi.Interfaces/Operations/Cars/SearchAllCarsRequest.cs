namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[Route("/cars", ServiceOperation.Search)]
public class SearchAllCarsRequest : SearchRequest<SearchAllCarsResponse>
{
}