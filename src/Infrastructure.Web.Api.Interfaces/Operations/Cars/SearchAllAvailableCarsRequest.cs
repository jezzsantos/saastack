namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

[Route("/cars/available", ServiceOperation.Search)]
public class SearchAllAvailableCarsRequest : TenantedSearchRequest<SearchAllCarsResponse>
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}