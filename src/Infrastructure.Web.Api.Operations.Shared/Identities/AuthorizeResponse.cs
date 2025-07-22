using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class AuthorizeResponse : IWebResponse
{
    public required string Code { get; set; }

    public string? State { get; set; }
}