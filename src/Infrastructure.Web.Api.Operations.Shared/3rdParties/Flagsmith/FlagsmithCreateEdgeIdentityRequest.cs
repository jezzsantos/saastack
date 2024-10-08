using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Creates a new edge identity
/// </summary>
[Route("/environments/{EnvironmentApiKey}/edge-identities/", OperationMethod.Post)]
[UsedImplicitly]
public class
    FlagsmithCreateEdgeIdentityRequest : WebRequest<FlagsmithCreateEdgeIdentityRequest,
    FlagsmithCreateEdgeIdentityResponse>
{
    public string? EnvironmentApiKey { get; set; }

    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("traits")] public List<FlagsmithTrait>? Traits { get; set; }
}