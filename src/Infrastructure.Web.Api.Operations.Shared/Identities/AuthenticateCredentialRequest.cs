using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authenticates a user with a username and password
/// </summary>
/// <response code="401">The user's username or password is invalid</response>
/// <response code="405">The user has not yet verified their registration</response>
/// <response code="403">
///     When the user has authenticated, but has MFA enabled. The details of the error contains a value of
///     "mfa_required".
/// </response>
/// <response code="423">The user's account is suspended or disabled, and cannot be authenticated or used</response>
[Route("/credentials/auth", OperationMethod.Post)]
public class AuthenticateCredentialRequest : UnTenantedRequest<AuthenticateCredentialRequest, AuthenticateResponse>
{
    [Required] public string? Password { get; set; }

    [Required] public string? Username { get; set; }
}