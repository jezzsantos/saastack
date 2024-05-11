using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/sso/auth", OperationMethod.Post)]
public class AuthenticateSingleSignOnRequest : UnTenantedRequest<AuthenticateResponse>
{
    [Required] public string? AuthCode { get; set; }

    public string? InvitationToken { get; set; }

    [Required] public string? Provider { get; set; }

    public string? Username { get; set; }
}