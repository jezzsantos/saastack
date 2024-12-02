using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithGetFeatureStatesResponse : IWebResponse
{
    [JsonPropertyName("results")]
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<FlagsmithFeatureState> Results { get; set; } = [];
}

[UsedImplicitly]
public class FlagsmithFeatureState
{
    public int Id { get; set; }
}