using Application.Interfaces.Resources;

namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

public class SearchAllCarsResponse : SearchResponse
{
    public List<Car>? Cars { get; set; }
}