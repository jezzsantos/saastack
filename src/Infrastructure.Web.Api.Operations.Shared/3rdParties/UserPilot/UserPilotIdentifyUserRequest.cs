using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.UserPilot;

/// <summary>
///     Identifies the user
/// </summary>
[Route("/identify", OperationMethod.Post)]
public class UserPilotIdentifyUserRequest : IWebRequest<EmptyResponse>
{
    [JsonPropertyName("company")] public Dictionary<string, string> Company { get; set; } = new();

    [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("user_id")] public string? UserId { get; set; }
}