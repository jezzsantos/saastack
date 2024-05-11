using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/tokens/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedRequest<RefreshTokenResponse>
{
    [Required] public string? RefreshToken { get; set; }
}