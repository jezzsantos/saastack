using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Notifies a Chargebee event, via a webhook
/// </summary>
[Route("/webhooks/chargebee", OperationMethod.Post)]
public class ChargebeeNotifyWebhookEventRequest : IWebRequest<EmptyResponse>
{
    public ChargebeeEventContent Content { get; set; } = new();

    [JsonPropertyName("event_type")] public string? EventType { get; set; }

    public string? Id { get; set; }
}