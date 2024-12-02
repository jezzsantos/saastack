using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class AuthenticateResponse : IWebResponse
{
    public required AuthenticateTokens Tokens { get; set; }
}