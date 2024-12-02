using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithGetEnvironmentFlagsResponse : List<FlagsmithFlag>, IWebResponse
{
    [UsedImplicitly]
    public FlagsmithGetEnvironmentFlagsResponse()
    {
    }

    public FlagsmithGetEnvironmentFlagsResponse(List<FlagsmithFlag> flags) : base(flags)
    {
    }
}

public class FlagsmithFlag
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }

    [JsonPropertyName("feature")] public required FlagsmithFeature Feature { get; set; }

    [JsonPropertyName("id")] public required int Id { get; set; }

    [JsonPropertyName("feature_state_value")]
    public required string Value { get; set; }
}

public class FlagsmithFeature
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }
}