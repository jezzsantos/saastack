#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Lists all the periods for when the specified car is unavailable
/// </summary>
[Route("/cars/{Id}/unavailabilities", OperationMethod.Search, isTestingOnly: true)]
public class SearchAllCarUnavailabilitiesRequest : TenantedSearchRequest<SearchAllCarUnavailabilitiesRequest,
    SearchAllCarUnavailabilitiesResponse>
{
    [Required] public string? Id { get; set; }
}
#endif