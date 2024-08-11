using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

/// <summary>
///     Exchanges an OAuth2 code for tokens
/// </summary>
[Route("/auth/token", OperationMethod.Post)]
public class
    ExchangeOAuth2CodeForTokensRequest : WebRequest<ExchangeOAuth2CodeForTokensRequest,
    ExchangeOAuth2CodeForTokensResponse>
{
    [JsonPropertyName("client_id")] public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }

    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("grant_type")] public string? GrantType { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }
}