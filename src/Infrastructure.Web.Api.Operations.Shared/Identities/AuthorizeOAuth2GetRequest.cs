using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application in Open ID Connect
/// </summary>
/// <response code="302">
///     The user is redirected to the redirect_uri with the authorization code.
///     Or if failed, if unauthenticated, the redirected to the login website page,
///     or if un-consented to the specified client, then redirected to the consent website page,
///     or if an error occurred, then redirected to the redirect_uri with an error and description.
/// </response>
[Route("/oauth2/authorize", OperationMethod.Get)]
public class AuthorizeOAuth2GetRequest : UnTenantedEmptyRequest<AuthorizeOAuth2GetRequest>
{
    [JsonPropertyName("client_id")] public string? ClientId { get; set; }

    [JsonPropertyName("code_challenge")] public string? CodeChallenge { get; set; }

    [JsonPropertyName("code_challenge_method")]
    [AllowedValues("plain", "s256")]
    // HACK: We cannot reliably use an enumeration here because of case-sensitivity
    public string? CodeChallengeMethod { get; set; }

    [JsonPropertyName("nonce")] public string? Nonce { get; set; }

    [JsonPropertyName("redirect_uri")]
    [Required]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("response_type")]
    [AllowedValues("code", "id_token", "token")]
    [Required]
    // HACK: We cannot reliably use an enumeration here because of case-sensitivity
    public string? ResponseType { get; set; }

    [JsonPropertyName("scope")] [Required] public string? Scope { get; set; }

    [JsonPropertyName("state")] public string? State { get; set; }
}