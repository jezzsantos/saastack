using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

public class StubWebhookNotificationAuditService : IWebhookNotificationAuditService
{
    public WebhookNotificationAudit? LastCreated { get; private set; }

    public WebhookNotificationAudit? LastProcessed { get; private set; }

    public Task<Result<WebhookNotificationAudit, Error>> CreateAuditAsync(ICallerContext caller, string source,
        string eventId, string eventType, string? jsonContent,
        CancellationToken cancellationToken)
    {
        var audit = new WebhookNotificationAudit
        {
            Id = Guid.NewGuid().ToString(),
            Source = source,
            EventId = eventId,
            EventType = eventType,
            JsonContent = jsonContent,
            Status = WebhookNotificationStatus.Received
        };
        LastCreated = audit;

        return Task.FromResult<Result<WebhookNotificationAudit, Error>>(audit);
    }

    public Task<Result<WebhookNotificationAudit, Error>> MarkAsFailedProcessingAsync(ICallerContext caller,
        string auditId, CancellationToken cancellationToken)
    {
        var audit = new WebhookNotificationAudit
        {
            Id = auditId,
            Source = LastCreated!.Source,
            EventId = LastCreated!.EventId,
            EventType = LastCreated!.EventType,
            JsonContent = LastCreated!.JsonContent,
            Status = WebhookNotificationStatus.Failed
        };

        return Task.FromResult<Result<WebhookNotificationAudit, Error>>(audit);
    }

    public Task<Result<WebhookNotificationAudit, Error>> MarkAsProcessedAsync(ICallerContext caller, string auditId,
        CancellationToken cancellationToken)
    {
        var audit = new WebhookNotificationAudit
        {
            Id = auditId,
            Source = LastCreated!.Source,
            EventId = LastCreated!.EventId,
            EventType = LastCreated!.EventType,
            JsonContent = LastCreated!.JsonContent,
            Status = WebhookNotificationStatus.Processed
        };
        LastProcessed = audit;

        return Task.FromResult<Result<WebhookNotificationAudit, Error>>(audit);
    }

    public void Reset()
    {
        LastCreated = null;
        LastProcessed = null;
    }
}