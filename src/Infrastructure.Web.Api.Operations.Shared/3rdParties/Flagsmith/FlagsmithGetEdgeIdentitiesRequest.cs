using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Fetches all edge identities
/// </summary>
[Route("/environments/{EnvironmentApiKey}/edge-identities/", OperationMethod.Get)]
public class
    FlagsmithGetEdgeIdentitiesRequest : WebRequest<FlagsmithGetEdgeIdentitiesRequest,
    FlagsmithGetEdgeIdentitiesResponse>
{
    public string? EnvironmentApiKey { get; set; }
}