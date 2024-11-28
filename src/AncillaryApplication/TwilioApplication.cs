using System.Globalization;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;

namespace AncillaryApplication;

public class TwilioApplication : ITwilioApplication
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly IRecorder _recorder;
    private readonly IWebhookNotificationAuditService _webHookNotificationAuditService;

    public TwilioApplication(IRecorder recorder, IAncillaryApplication ancillaryApplication,
        IWebhookNotificationAuditService webHookNotificationAuditService)
    {
        _recorder = recorder;
        _ancillaryApplication = ancillaryApplication;
        _webHookNotificationAuditService = webHookNotificationAuditService;
    }

    public async Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, TwilioEventData eventData,
        CancellationToken cancellationToken)
    {
        var eventId = $"{eventData.MessageSid}-{Guid.NewGuid():N}";
        var eventStatus = eventData.MessageStatus;
        var @event = eventStatus.ToEnumOrDefault(TwilioMessageStatus.Unknown);
        _recorder.TraceInformation(caller.ToCall(), "Twilio webhook event received: {Event} for {Status}", eventId,
            @event);

        var created = await _webHookNotificationAuditService.CreateAuditAsync(caller, TwilioConstants.AuditSourceName,
            eventId, eventStatus.ToString(), eventData.ToJson(false), cancellationToken);
        if (created.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to audit Twilio webhook event {Event} for {Status} with {ErrorCode}: {Message}", eventId,
                eventStatus, created.Error.Code, created.Error.Message);
            return created.Error;
        }

        var audit = created.Value;
        switch (@event)
        {
            case TwilioMessageStatus.Delivered:
            {
                var deliveredAt = eventData.RawDlrDoneDate.HasValue
                    ? eventData.RawDlrDoneDate.Value.FromTwilioDateLong()
                    : DateTime.UtcNow;
                var receiptId = eventData.MessageSid;

                return await ConfirmSmsDeliveryAsync(caller, audit, receiptId, deliveredAt, cancellationToken);
            }

            case TwilioMessageStatus.Failed:
            {
                var failedAt = DateTime.UtcNow;
                var reason = eventData.ErrorCode ?? "none";
                var receiptId = eventData.MessageSid;

                return await ConfirmSmsDeliveryFailedAsync(caller, audit, receiptId, failedAt, reason,
                    cancellationToken);
            }

            default:
                _recorder.TraceInformation(caller.ToCall(), "Twilio webhook event ignored: {Event} for {Status}",
                    eventId, @event);
                return Result.Ok;
        }
    }

    private async Task<Result<Error>> ConfirmSmsDeliveryAsync(ICallerContext caller, WebhookNotificationAudit audit,
        string receiptId, DateTime deliveredAt, CancellationToken cancellationToken)
    {
        var delivered =
            await _ancillaryApplication.ConfirmSmsDeliveredAsync(caller, receiptId, deliveredAt, cancellationToken);
        if (delivered.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to confirm delivery for Twilio receipt {Receipt}, with {ErrorCode}: {Message}",
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

    private async Task<Result<Error>> ConfirmSmsDeliveryFailedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string receiptId, DateTime failedAt, string reason,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmSmsDeliveryFailedAsync(caller,
            receiptId, failedAt, reason, cancellationToken);
        if (delivered.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to confirm failed delivery for Twilio receipt {Receipt}, with {ErrorCode}: {Message}",
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

public static class TwilioApplicationConversionExtensions
{
    public static DateTime FromTwilioDateLong(this long twilioDate)
    {
        return DateTime.ParseExact(twilioDate.ToString().PadLeft(10), "yyMMddHHmm",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    public static long ToTwilioDateLong(this DateTime dateTime)
    {
        return dateTime
            .ToNearestMinute()
            .ToString("yyMMddHHmm")
            .PadLeft(10)
            .ToLong();
    }
}