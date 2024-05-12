using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Fetches the named feature flag for the current authenticated user
/// </summary>
[Route("/flags/{Name}", OperationMethod.Get)]
public class GetFeatureFlagForCallerRequest : UnTenantedRequest<GetFeatureFlagResponse>
{
    [Required] public string? Name { get; set; }
}