using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/flags/{Name}", OperationMethod.Get)]
public class GetFeatureFlagForCallerRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    [Required] public string? Name { get; set; }
}