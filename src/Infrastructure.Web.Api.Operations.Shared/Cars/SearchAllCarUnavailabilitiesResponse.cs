using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.Cars;

public class SearchAllCarUnavailabilitiesResponse : SearchResponse
{
    public List<Unavailability>? Unavailabilities { get; set; }
}
#endif