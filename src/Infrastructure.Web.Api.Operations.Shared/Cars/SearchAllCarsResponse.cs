using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

public class SearchAllCarsResponse : SearchResponse
{
    public List<Car> Cars { get; set; } = [];
}