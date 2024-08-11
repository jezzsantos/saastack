using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authenticates a user with a single sign-on provider (also auto-registering them the first time)
/// </summary>
/// <response code="401">The provider is not known, or the code is invalid</response>
/// <response code="409">The user's account is suspended or locked, and cannot be authenticated or used</response>
[Route("/sso/auth", OperationMethod.Post)]
public class AuthenticateSingleSignOnRequest : UnTenantedRequest<AuthenticateSingleSignOnRequest, AuthenticateResponse>
{
    [Required] public string? AuthCode { get; set; }

    public string? InvitationToken { get; set; }

    [Required] public string? Provider { get; set; }

    public string? Username { get; set; }
}