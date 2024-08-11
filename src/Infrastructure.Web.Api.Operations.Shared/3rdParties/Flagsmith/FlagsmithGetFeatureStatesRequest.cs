using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Fetches the feature states
/// </summary>
[Route("/environments/{EnvironmentApiKey}/featurestates/", OperationMethod.Get)]
public class
    FlagsmithGetFeatureStatesRequest : WebRequest<FlagsmithGetFeatureStatesRequest, FlagsmithGetFeatureStatesResponse>
{
    public string? EnvironmentApiKey { get; set; }

    [JsonPropertyName("feature")] public int? Feature { get; set; }
}