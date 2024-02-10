using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class AuthenticateResponse : IWebResponse
{
    public string? AccessToken { get; set; }

    public DateTime? AccessTokenExpiresOnUtc { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresOnUtc { get; set; }

    public string? UserId { get; set; }
}