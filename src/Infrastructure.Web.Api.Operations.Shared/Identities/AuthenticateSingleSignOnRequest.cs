using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authenticates a user with authorization code from a OAuth2 or Open ID Connect single sign-on provider, and
///     auto-registering them the first time
/// </summary>
/// <response code="401">The provider is not known, or the authorization code is invalid</response>
/// <response code="423">The user's account is suspended or disabled, and cannot be authenticated or used</response>
[Route("/sso/auth", OperationMethod.Post)]
public class AuthenticateSingleSignOnRequest : UnTenantedRequest<AuthenticateSingleSignOnRequest, AuthenticateResponse>
{
    [Required] public string? AuthCode { get; set; }

    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; set; }

    public string? InvitationToken { get; set; }

    [Required] public string? Provider { get; set; }

    public bool? TermsAndConditionsAccepted { get; set; }

    public string? Username { get; set; }
}