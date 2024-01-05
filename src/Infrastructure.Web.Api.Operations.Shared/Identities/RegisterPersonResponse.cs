using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegisterPersonResponse : IWebResponse
{
    public PasswordCredential? Credential { get; set; }
}