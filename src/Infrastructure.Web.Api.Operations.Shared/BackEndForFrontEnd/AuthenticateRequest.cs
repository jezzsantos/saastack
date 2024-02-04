using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/auth", ServiceOperation.Post)]
public class AuthenticateRequest : UnTenantedRequest<AuthenticateResponse>
{
    public string? AuthCode { get; set; }

    public string? Password { get; set; }

    public required string Provider { get; set; }

    public string? Username { get; set; }
}