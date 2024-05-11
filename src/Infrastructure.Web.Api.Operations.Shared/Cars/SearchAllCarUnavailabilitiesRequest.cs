#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{Id}/unavailabilities", OperationMethod.Search, isTestingOnly: true)]
public class SearchAllCarUnavailabilitiesRequest : TenantedSearchRequest<SearchAllCarUnavailabilitiesResponse>
{
    [Required] public string? Id { get; set; }
}
#endif