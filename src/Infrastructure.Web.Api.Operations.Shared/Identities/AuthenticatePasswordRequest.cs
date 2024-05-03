using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authenticates a user with a username and password
/// </summary>
/// <response code="401">The user's username or password is invalid</response>
/// <response code="405">The user has not yet verified their registration</response>
/// <response code="409">The user's account is suspended or locked, and cannot be authenticated or used</response>
[Route("/passwords/auth", OperationMethod.Post)]
public class AuthenticatePasswordRequest : UnTenantedRequest<AuthenticateResponse>
{
    public required string Password { get; set; }

    public required string Username { get; set; }
}