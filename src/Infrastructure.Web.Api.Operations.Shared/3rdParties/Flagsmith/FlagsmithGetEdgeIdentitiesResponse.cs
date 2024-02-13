using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithGetEdgeIdentitiesResponse : IWebResponse
{
    [JsonPropertyName("results")] public List<FlagsmithEdgeIdentity> Results { get; set; } = new();
}

[UsedImplicitly]
public class FlagsmithEdgeIdentity
{
    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("identity_uuid")] public string? IdentityUuid { get; set; }
}