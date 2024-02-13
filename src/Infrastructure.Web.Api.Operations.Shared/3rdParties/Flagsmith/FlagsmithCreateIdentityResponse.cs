using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithCreateIdentityResponse : IWebResponse
{
    [JsonPropertyName("flags")] public List<FlagsmithFlag> Flags { get; set; } = new();

    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("traits")] public List<FlagsmithTrait> Traits { get; set; } = new();
}

[UsedImplicitly]
public class FlagsmithTrait
{
    [JsonPropertyName("trait_key")] public string? Key { get; set; }

    [JsonPropertyName("trait_value")] public object? Value { get; set; }
}