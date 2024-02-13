using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithCreateEdgeIdentityResponse : IWebResponse
{
    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("identity_uuid")] public string? IdentityUuid { get; set; }
}