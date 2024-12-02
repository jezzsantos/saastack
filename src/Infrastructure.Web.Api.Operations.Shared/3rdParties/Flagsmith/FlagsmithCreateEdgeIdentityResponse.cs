using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

public class FlagsmithCreateEdgeIdentityResponse : IWebResponse
{
    [JsonPropertyName("identifier")] public required string Identifier { get; set; }

    [JsonPropertyName("identity_uuid")] public required string IdentityUuid { get; set; }
}