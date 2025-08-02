using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the access_token for the specified grant_type for OpenID Connect
/// </summary>
[Route("/oauth2/token", OperationMethod.Post)]
public class CreateOAuth2TokenRequest : UnTenantedRequest<CreateOAuth2TokenRequest, CreateOAuthTokenResponse>
{
    [JsonPropertyName("grant_type")]
    [Required]
    public OAuth2GrantType? GrantType { get; set; }

    [JsonPropertyName("client_id")]
    [Required]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    [Required]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; set; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }
}