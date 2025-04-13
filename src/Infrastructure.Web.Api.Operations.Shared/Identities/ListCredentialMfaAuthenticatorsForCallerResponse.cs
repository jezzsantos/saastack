using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ListCredentialMfaAuthenticatorsForCallerResponse : IWebResponse
{
    public List<CredentialMfaAuthenticator> Authenticators { get; set; } = [];
}