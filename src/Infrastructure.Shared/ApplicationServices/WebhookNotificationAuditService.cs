using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service to audit webhook notification events
/// </summary>
public class WebhookNotificationAuditService : IWebhookNotificationAuditService
{
    private const string IdPrefix = "webhookevt";
    private readonly IRecorder _recorder;
    private readonly IWebhookNotificationAuditRepository _repository;

    public WebhookNotificationAuditService(IRecorder recorder, IWebhookNotificationAuditRepository repository)
    {
        _recorder = recorder;
        _repository = repository;
    }

    public async Task<Result<WebhookNotificationAudit, Error>> CreateAuditAsync(ICallerContext caller, string source,
        string eventId, string eventType, string? jsonContent,
        CancellationToken cancellationToken)
    {
        var id = CreateIdentifier(source, eventId);
        var audit = new Application.Persistence.Shared.ReadModels.WebhookNotificationAudit
        {
            Id = id,
            Source = source,
            EventId = eventId,
            EventType = eventType,
            Status = WebhookNotificationStatus.Received,
            JsonContent = jsonContent
        };

        var created = await _repository.SaveAsync(audit, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        audit = created.Value;
        _recorder.TraceInformation(caller.ToCall(), "Webhook notification audit {Id} was created", id);
        return audit.ToAudit();
    }

    public async Task<Result<WebhookNotificationAudit, Error>> MarkAsFailedProcessingAsync(ICallerContext caller,
        string auditId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(auditId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var audit = retrieved.Value;
        audit.Status = WebhookNotificationStatus.Failed;

        var saved = await _repository.SaveAsync(audit, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        audit = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Webhook notification audit {Id} failed processing", audit.Id);
        return audit.ToAudit();
    }

    public async Task<Result<WebhookNotificationAudit, Error>> MarkAsProcessedAsync(ICallerContext caller,
        string auditId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(auditId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var audit = retrieved.Value;
        audit.Status = WebhookNotificationStatus.Processed;

        var saved = await _repository.SaveAsync(audit, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        audit = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Webhook notification audit {Id} was processed successfully",
            audit.Id);
        return audit.ToAudit();
    }

    private static string CreateIdentifier(string source, string eventId)
    {
        var random = Guid.NewGuid().ToString("N").Substring(0, 24);

        return $"{IdPrefix}_{source}.{eventId}.{random}";
    }
}

internal static class WebhookNotificationAuditConversionExtensions
{
    public static WebhookNotificationAudit ToAudit(
        this Application.Persistence.Shared.ReadModels.WebhookNotificationAudit audit)
    {
        return new WebhookNotificationAudit
        {
            Id = audit.Id,
            Source = audit.Source,
            EventId = audit.EventId,
            EventType = audit.EventType,
            JsonContent = audit.JsonContent,
            Status = audit.Status
        };
    }
}