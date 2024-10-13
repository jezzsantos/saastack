using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class WebhookNotificationAudit : IIdentifiableResource
{
    public required string EventId { get; set; }

    public required string EventType { get; set; }

    public string? JsonContent { get; set; }

    public required string Source { get; set; }

    public WebhookNotificationStatus Status { get; set; }

    public required string Id { get; set; }
}

public enum WebhookNotificationStatus
{
    Received = 0,
    Processed = 1,
    Failed = 2
}