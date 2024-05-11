using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/environments/{EnvironmentApiKey}/featurestates/{FeatureStateId}/", OperationMethod.PutPatch)]
public class FlagsmithCreateFeatureStateRequest : IWebRequest<FlagsmithCreateFeatureStateResponse>
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }

    public string? EnvironmentApiKey { get; set; }

    [JsonPropertyName("feature")] public int Feature { get; set; }

    public int FeatureStateId { get; set; }
}