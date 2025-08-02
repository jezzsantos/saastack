using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Authorizes the user to access the application in Open ID Connect
/// </summary>
[Interfaces.Route("/oauth2/authorize", OperationMethod.Get)]
public class AuthorizeOAuth2GetRequest : UnTenantedEmptyRequest<AuthorizeOAuth2GetRequest>
{
    [FromQuery(Name = "client_id")]
    [Required]
    public string? ClientId { get; set; }

    [FromQuery(Name = "code_challenge")] public string? CodeChallenge { get; set; }

    [FromQuery(Name = "code_challenge_method")]
    public OpenIdConnectCodeChallengeMethod? CodeChallengeMethod { get; set; }

    [FromQuery(Name = "nonce")] public string? Nonce { get; set; }

    [FromQuery(Name = "redirect_uri")]
    [Required]
    public string? RedirectUri { get; set; }

    [FromQuery(Name = "response_type")]
    [Required]
    public string? ResponseType { get; set; } // HACK: We cannot use an enumeration here

    [FromQuery(Name = "scope")] [Required] public string? Scope { get; set; }

    [FromQuery(Name = "state")] public string? State { get; set; }
}