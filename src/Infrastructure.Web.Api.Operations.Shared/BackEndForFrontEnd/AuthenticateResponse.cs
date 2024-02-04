using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class AuthenticateResponse : IWebResponse
{
    public string? UserId { get; set; }
}