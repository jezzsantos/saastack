using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/environments/{EnvironmentApiKey}/featurestates/", ServiceOperation.Post)]
public class FlagsmithGetFeatureStatesRequest : IWebRequest<FlagsmithGetFeatureStatesResponse>
{
    public required string EnvironmentApiKey { get; set; }

    [JsonPropertyName("feature")] public int Feature { get; set; }
}