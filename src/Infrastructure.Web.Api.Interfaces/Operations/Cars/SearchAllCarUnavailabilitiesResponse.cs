#if TESTINGONLY
using Application.Interfaces.Resources;

namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

public class SearchAllCarUnavailabilitiesResponse : SearchResponse
{
    public List<Unavailability>? Unavailabilities { get; set; }
}
#endif