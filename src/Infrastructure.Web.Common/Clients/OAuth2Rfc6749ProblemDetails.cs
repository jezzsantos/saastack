using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     Defines an OAuth2 error, from <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-5.2">RFC6749</see>
/// </summary>
[UsedImplicitly]
public class OAuth2Rfc6749ProblemDetails
{
    public const string Reference = "https://datatracker.ietf.org/doc/html/rfc6749#section-5.2";

    [JsonPropertyName("error")] public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")] public string? ErrorUri { get; set; }

    [JsonPropertyName("state")] public string? State { get; set; }
}