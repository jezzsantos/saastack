using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class AssociatePasswordMfaAuthenticatorForCallerResponse : IWebResponse
{
    public PasswordCredentialMfaAssociation? Authenticator { get; set; }
}