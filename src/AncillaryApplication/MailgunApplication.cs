using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;

namespace AncillaryApplication;

/// <summary>
///     We want to avoid raising errors for failed attempts here, so that Mailgun does not attempt to retry again
/// </summary>
public class MailgunApplication : IMailgunApplication
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly IRecorder _recorder;
    private readonly IWebhookNotificationAuditService _webHookNotificationAuditService;

    public MailgunApplication(IRecorder recorder, IAncillaryApplication ancillaryApplication,
        IWebhookNotificationAuditService webHookNotificationAuditService)
    {
        _recorder = recorder;
        _ancillaryApplication = ancillaryApplication;
        _webHookNotificationAuditService = webHookNotificationAuditService;
    }

    public async Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, MailgunEventData eventData,
        CancellationToken cancellationToken)
    {
        var eventType = eventData.Event!;
        var @event = eventType.ToEnumOrDefault(MailgunEventType.Unknown);

        _recorder.TraceInformation(caller.ToCall(), "Mailgun webhook event received: {Event}", eventType);

        var created = await _webHookNotificationAuditService.CreateAuditAsync(caller, MailgunConstants.AuditSourceName,
            eventData.Id!, eventType, eventData.ToJson(false), cancellationToken);
        if (created.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to audit Mailgun webhook event {Event} with {ErrorCode}: {Message}", eventType,
                created.Error.Code,
                created.Error.Message);
            return created.Error;
        }

        var audit = created.Value;
        switch (@event)
        {
            case MailgunEventType.Delivered:
            {
                var deliveredAt = eventData.Timestamp.FromUnixTimestamp();
                var receiptId = eventData.Message?.Headers?.MessageId;
                if (receiptId.HasNoValue())
                {
                    return Result.Ok;
                }

                return await ConfirmEmailDeliveryAsync(caller, audit, receiptId, deliveredAt, cancellationToken);
            }

            case MailgunEventType.Failed:
            {
                var severity = eventData.Severity ?? MailgunConstants.Values.PermanentSeverity;
                if (severity.NotEqualsIgnoreCase(MailgunConstants.Values.PermanentSeverity))
                {
                    return Result.Ok;
                }

                var failedAt = eventData.Timestamp.FromUnixTimestamp();
                var reason = eventData.DeliveryStatus?.Description ?? eventData.Reason ?? "none";
                var receiptId = eventData.Message?.Headers?.MessageId;
                if (receiptId.HasNoValue())
                {
                    return Result.Ok;
                }

                return await ConfirmEmailDeliveryFailedAsync(caller, audit, receiptId, failedAt, reason,
                    cancellationToken);
            }

            default:
                _recorder.TraceInformation(caller.ToCall(), "Mailgun webhook event ignored: {Event}", eventType);
                return Result.Ok;
        }
    }

    private async Task<Result<Error>> ConfirmEmailDeliveryAsync(ICallerContext caller, WebhookNotificationAudit audit,
        string receiptId, DateTime deliveredAt, CancellationToken cancellationToken)
    {
        var delivered =
            await _ancillaryApplication.ConfirmEmailDeliveredAsync(caller, receiptId, deliveredAt, cancellationToken);
        if (delivered.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to confirm delivery for Mailgun receipt {Receipt}, with {ErrorCode}: {Message}",
                receiptId, delivered.Error.Code, delivered.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return delivered.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> ConfirmEmailDeliveryFailedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string receiptId, DateTime failedAt, string reason,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmEmailDeliveryFailedAsync(caller,
            receiptId, failedAt, reason, cancellationToken);
        if (delivered.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to confirm failed delivery for Mailgun receipt {Receipt}, with {ErrorCode}: {Message}",
                receiptId, delivered.Error.Code, delivered.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return delivered.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }
}