using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ChallengeCredentialMfaAuthenticatorForCallerResponse : IWebResponse
{
    public required CredentialMfaAuthenticatorChallenge Challenge { get; set; }
}