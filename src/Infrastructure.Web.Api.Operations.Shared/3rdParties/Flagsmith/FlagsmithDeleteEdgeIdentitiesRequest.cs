using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Deletes an edge identity
/// </summary>
[Route("/environments/{EnvironmentApiKey}/edge-identities/{IdentityUuid}/", OperationMethod.Delete)]
public class FlagsmithDeleteEdgeIdentitiesRequest : IWebRequest<EmptyResponse>
{
    public string? EnvironmentApiKey { get; set; }

    public string? IdentityUuid { get; set; }
}