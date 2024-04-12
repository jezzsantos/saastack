using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/environments/{EnvironmentApiKey}/edge-identities/{IdentityUuid}/", OperationMethod.Delete)]
public class FlagsmithDeleteEdgeIdentitiesRequest : IWebRequest<EmptyResponse>
{
    public required string EnvironmentApiKey { get; set; }

    public required string IdentityUuid { get; set; }
}