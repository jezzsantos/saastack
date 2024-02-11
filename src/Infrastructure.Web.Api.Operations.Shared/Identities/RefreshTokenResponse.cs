using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RefreshTokenResponse : IWebResponse
{
    public AuthenticateTokens? Tokens { get; set; }
}