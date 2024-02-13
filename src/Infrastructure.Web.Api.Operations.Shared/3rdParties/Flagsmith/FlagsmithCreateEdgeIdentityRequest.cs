using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/environments/{EnvironmentApiKey}/edge-identities/", ServiceOperation.Post)]
[UsedImplicitly]
public class FlagsmithCreateEdgeIdentityRequest : IWebRequest<FlagsmithCreateEdgeIdentityResponse>
{
    public required string EnvironmentApiKey { get; set; }

    [JsonPropertyName("identifier")] public required string Identifier { get; set; }

    [JsonPropertyName("traits")] public List<FlagsmithTrait>? Traits { get; set; }
}