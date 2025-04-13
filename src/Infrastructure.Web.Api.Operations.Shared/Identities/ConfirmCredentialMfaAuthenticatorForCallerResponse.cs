using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ConfirmCredentialMfaAuthenticatorForCallerResponse : IWebResponse
{
    public List<CredentialMfaAuthenticator>? Authenticators { get; set; }

    public AuthenticateTokens? Tokens { get; set; }
}