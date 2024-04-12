using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/environments/{EnvironmentApiKey}/edge-identities/", OperationMethod.Get)]
public class FlagsmithGetEdgeIdentitiesRequest : IWebRequest<FlagsmithGetEdgeIdentitiesResponse>
{
    public required string EnvironmentApiKey { get; set; }
}