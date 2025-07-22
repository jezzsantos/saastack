using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the access_token for the specified grant_type for OpenID Connect
/// </summary>
[Route("/oauth2/token", OperationMethod.Post)]
public class TokenEndpointRequest : UnTenantedRequest<TokenEndpointRequest, TokenEndpointResponse>
{
    public string? GrantType { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Code { get; set; }

    public string? RedirectUri { get; set; }

    public string? CodeVerifier { get; set; }

    public string? RefreshToken { get; set; }

    public string? Scope { get; set; }
}