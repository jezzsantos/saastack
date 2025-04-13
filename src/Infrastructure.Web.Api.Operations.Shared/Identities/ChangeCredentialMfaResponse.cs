using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class ChangeCredentialMfaResponse : IWebResponse
{
    public required PersonCredential Credential { get; set; }
}