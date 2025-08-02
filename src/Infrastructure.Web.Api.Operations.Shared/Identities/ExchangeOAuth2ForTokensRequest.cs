using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Exchanges either an authorization code for new tokens, or a refresh_token for new tokens,
///     for the specified grant_type in Open ID Connect
/// </summary>
[Route("/oauth2/token", OperationMethod.Post)]
public class ExchangeOAuth2ForTokensRequest :
    UnTenantedRequest<ExchangeOAuth2ForTokensRequest, ExchangeOAuth2ForTokensResponse>, IHasFormUrlEncoded
{
    [JsonPropertyName("client_id")]
    [Required]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    [Required]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; set; }

    [JsonPropertyName("grant_type")]
    [Required]
    public OAuth2GrantType? GrantType { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }
}