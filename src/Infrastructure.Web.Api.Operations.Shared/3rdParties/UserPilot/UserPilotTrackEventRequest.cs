using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.UserPilot;

/// <summary>
///     Tracks the event
/// </summary>
[Route("/track", OperationMethod.Post)]
public class UserPilotTrackEventRequest : IWebRequest<EmptyResponse>
{
    [JsonPropertyName("event_name")] public string? EventName { get; set; }

    [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("user_id")] public string? UserId { get; set; }
}