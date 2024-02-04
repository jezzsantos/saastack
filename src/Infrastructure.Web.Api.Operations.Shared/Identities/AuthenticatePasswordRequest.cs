using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/auth", ServiceOperation.Post)]
public class AuthenticatePasswordRequest : UnTenantedRequest<AuthenticateResponse>
{
    public required string Password { get; set; }

    public required string Username { get; set; }
}