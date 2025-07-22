using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class TokenEndpointResponse : IWebResponse
{
    public required string AccessToken { get; set; }

    public required string TokenType { get; set; }

    public required int ExpiresIn { get; set; }

    public string? RefreshToken { get; set; }

    public string? IdToken { get; set; }

    public string? Scope { get; set; }
}