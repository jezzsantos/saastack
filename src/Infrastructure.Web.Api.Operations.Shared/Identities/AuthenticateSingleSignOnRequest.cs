using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/sso/auth", ServiceOperation.Post)]
public class AuthenticateSingleSignOnRequest : UnTenantedRequest<AuthenticateResponse>
{
    public required string AuthCode { get; set; }

    public string? InvitationToken { get; set; }

    public required string Provider { get; set; }

    public string? Username { get; set; }
}