using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for auditing received inbound webhook notifications.
/// </summary>
public interface IWebhookNotificationAuditService
{
    Task<Result<WebhookNotificationAudit, Error>> CreateAuditAsync(ICallerContext caller, string source, string eventId,
        string eventType, string? jsonContent, CancellationToken cancellationToken);

    Task<Result<WebhookNotificationAudit, Error>> MarkAsFailedProcessingAsync(ICallerContext caller, string auditId,
        CancellationToken cancellationToken);

    Task<Result<WebhookNotificationAudit, Error>> MarkAsProcessedAsync(ICallerContext caller, string auditId,
        CancellationToken cancellationToken);
}