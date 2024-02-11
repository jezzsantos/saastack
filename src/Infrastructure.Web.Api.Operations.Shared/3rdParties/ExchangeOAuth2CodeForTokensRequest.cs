using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties;

[Route("/auth/token", ServiceOperation.Post)]
public class ExchangeOAuth2CodeForTokensRequest : UnTenantedRequest<ExchangeOAuth2CodeForTokensResponse>
{
    [JsonPropertyName("client_id")] public required string ClientId { get; set; }

    [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }

    [JsonPropertyName("code")] public required string Code { get; set; }

    [JsonPropertyName("grant_type")] public required string GrantType { get; set; }

    [JsonPropertyName("redirect_uri")] public required string RedirectUri { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }
}