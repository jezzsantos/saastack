using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Authenticates the user with the specified provider, using either an auth code or a username and password.
/// </summary>
[Route("/auth", OperationMethod.Post)]
public class AuthenticateRequest : UnTenantedRequest<AuthenticateResponse>
{
    public string? AuthCode { get; set; }

    public string? Password { get; set; }

    [Required] public string? Provider { get; set; }

    public string? Username { get; set; }
}