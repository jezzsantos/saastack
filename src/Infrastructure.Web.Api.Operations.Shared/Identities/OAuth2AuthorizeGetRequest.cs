using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application with OpenId Connect
/// </summary>
[Interfaces.Route("/oauth2/authorize", OperationMethod.Get)]
public class OAuth2AuthorizeGetRequest : UnTenantedRequest<OAuth2AuthorizeGetRequest, AuthorizeResponse>
{
    [FromQuery] public string? ClientId { get; set; }

    [FromQuery] public string? CodeChallenge { get; set; }

    [FromQuery] public string? CodeChallengeMethod { get; set; }

    [FromQuery] public string? Nonce { get; set; }

    [FromQuery] public string? RedirectUri { get; set; }

    [FromQuery] public string? ResponseType { get; set; }

    [FromQuery] public string? Scope { get; set; }

    [FromQuery] public string? State { get; set; }
}