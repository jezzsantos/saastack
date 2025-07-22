using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application with OpenId Connect
/// </summary>
[Route("/oauth2/authorize", OperationMethod.Get)]
public class AuthorizeRequest : UnTenantedRequest<AuthorizeRequest, AuthorizeResponse>
{
    public string? ClientId { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseType { get; set; }

    public string? Scope { get; set; }

    public string? State { get; set; }

    public string? Nonce { get; set; }

    public string? CodeChallenge { get; set; }

    public string? CodeChallengeMethod { get; set; }
}