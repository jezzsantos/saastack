using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ChallengePasswordMfaAuthenticatorForCallerResponse : IWebResponse
{
    public required PasswordCredentialMfaAuthenticatorChallenge Challenge { get; set; }
}