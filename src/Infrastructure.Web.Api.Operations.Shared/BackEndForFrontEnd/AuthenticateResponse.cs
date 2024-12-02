using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class AuthenticateResponse : IWebResponse
{
    public required string UserId { get; set; }
}