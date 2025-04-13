using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegisterPersonCredentialResponse : IWebResponse
{
    public required PersonCredential Person { get; set; }
}