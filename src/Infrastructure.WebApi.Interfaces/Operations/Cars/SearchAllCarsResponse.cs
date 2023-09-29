using Application.Interfaces.Resources;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

public class SearchAllCarsResponse : SearchResponse
{
    public List<Car>? Cars { get; set; }
}