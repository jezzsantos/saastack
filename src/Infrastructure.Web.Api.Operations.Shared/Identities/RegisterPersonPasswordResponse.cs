using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegisterPersonPasswordResponse : IWebResponse
{
    public required PasswordCredential Credential { get; set; }
}