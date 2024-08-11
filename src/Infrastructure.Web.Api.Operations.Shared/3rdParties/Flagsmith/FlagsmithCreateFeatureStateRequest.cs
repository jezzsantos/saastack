using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Creates a new feature state for an edge identity.
/// </summary>
[Route("/environments/{EnvironmentApiKey}/featurestates/{FeatureStateId}/", OperationMethod.PutPatch)]
public class
    FlagsmithCreateFeatureStateRequest : WebRequest<FlagsmithCreateFeatureStateRequest,
    FlagsmithCreateFeatureStateResponse>
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }

    public string? EnvironmentApiKey { get; set; }

    [JsonPropertyName("feature")] public int Feature { get; set; }

    public int FeatureStateId { get; set; }
}