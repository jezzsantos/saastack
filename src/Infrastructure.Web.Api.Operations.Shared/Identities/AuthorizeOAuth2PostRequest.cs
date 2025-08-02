using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application in Open ID Connect
/// </summary>
[Route("/oauth2/authorize", OperationMethod.Post)]
public class AuthorizeOAuth2PostRequest : UnTenantedEmptyRequest<AuthorizeOAuth2PostRequest>,
    IHasFormUrlEncoded
{
    [JsonPropertyName("client_id")]
    [Required]
    public string? ClientId { get; set; }

    [JsonPropertyName("code_challenge")] public string? CodeChallenge { get; set; }

    [JsonPropertyName("code_challenge_method")]
    public OpenIdConnectCodeChallengeMethod? CodeChallengeMethod { get; set; }

    [JsonPropertyName("nonce")] public string? Nonce { get; set; }

    [JsonPropertyName("redirect_uri")]
    [Required]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("response_type")]
    [Required]
    public OAuth2ResponseType? ResponseType { get; set; }

    [JsonPropertyName("scope")] [Required] public string? Scope { get; set; }

    [JsonPropertyName("state")] public string? State { get; set; }
}