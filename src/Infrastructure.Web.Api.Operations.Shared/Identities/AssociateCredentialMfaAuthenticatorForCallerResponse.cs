using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class AssociateCredentialMfaAuthenticatorForCallerResponse : IWebResponse
{
    public required CredentialMfaAuthenticatorAssociation Authenticator { get; set; }
}