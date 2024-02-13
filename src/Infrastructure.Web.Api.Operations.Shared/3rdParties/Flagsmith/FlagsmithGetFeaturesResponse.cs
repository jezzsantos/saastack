using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithGetFeaturesResponse : IWebResponse
{
    [JsonPropertyName("results")] public List<FlagsmithFeature> Results { get; set; } = new();
}