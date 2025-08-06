using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ExchangeOAuth2ForTokensResponse : IWebResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")] public required int ExpiresIn { get; set; }

    [JsonPropertyName("id_token")] public string? IdToken { get; set; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

    [JsonPropertyName("token_type")] public required OAuth2TokenType TokenType { get; set; }
}