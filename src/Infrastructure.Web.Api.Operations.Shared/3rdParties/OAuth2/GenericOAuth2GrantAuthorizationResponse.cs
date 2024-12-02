using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

public class GenericOAuth2GrantAuthorizationResponse : IWebResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; } // seconds from now

    [JsonPropertyName("id_token")] public string? IdToken { get; set; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; } //MS specific

    [JsonPropertyName("scope")] public string? Scope { get; set; } //MS specific

    [JsonPropertyName("token_type")] public string? TokenType { get; set; } //i.e. bearer or Bearer
}