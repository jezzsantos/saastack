using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application with OpenId Connect
/// </summary>
[Route("/oauth2/authorize", OperationMethod.Post)]
public class OAuth2AuthorizePostRequest : UnTenantedRequest<OAuth2AuthorizePostRequest, AuthorizeResponse>,
    IHasFormUrlEncoded
{
    public string? ClientId { get; set; }

    public string? CodeChallenge { get; set; }

    public string? CodeChallengeMethod { get; set; }

    public string? Nonce { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseType { get; set; }

    public string? Scope { get; set; }

    public string? State { get; set; }
}