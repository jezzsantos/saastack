using Application.Persistence.Common;
using Application.Resources.Shared;
using Common;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName("WebhookNotificationAudits")]
public class WebhookNotificationAudit : ReadModelEntity
{
    public Optional<string> EventId { get; set; }

    public Optional<string> EventType { get; set; }

    public Optional<string> JsonContent { get; set; }

    public Optional<string> Source { get; set; }

    public WebhookNotificationStatus Status { get; set; }
}