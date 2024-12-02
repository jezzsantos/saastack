using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithCreateIdentityResponse : IWebResponse
{
    [JsonPropertyName("flags")] public List<FlagsmithFlag> Flags { get; set; } = [];

    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("traits")] public List<FlagsmithTrait> Traits { get; set; } = [];
}

[UsedImplicitly]
public class FlagsmithTrait
{
    [JsonPropertyName("trait_key")] public required string Key { get; set; }

    [JsonPropertyName("trait_value")] public required object Value { get; set; }
}