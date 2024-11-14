using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Shared;

namespace AncillaryApplication;

partial class AncillaryApplication
{
    public async Task<Result<Error>> ConfirmEmailDeliveredAsync(ICallerContext caller, string receiptId,
        DateTime deliveredAt, CancellationToken cancellationToken)
    {
        var retrieved = await _emailDeliveryRepository.FindByReceiptIdAsync(receiptId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Result.Ok;
        }

        var email = retrieved.Value.Value;
        var delivered = email.ConfirmDelivery(receiptId, deliveredAt);
        if (delivered.IsFailure)
        {
            if (delivered.Error.Is(ErrorCode.RuleViolation))
            {
                return Result.Ok;
            }

            return delivered.Error;
        }

        var saved = await _emailDeliveryRepository.SaveAsync(email, false, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        email = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Email {Receipt} confirmed delivered for {For}",
            receiptId, email.Recipient.Value.EmailAddress.Address);

        return Result.Ok;
    }

    public async Task<Result<Error>> ConfirmEmailDeliveryFailedAsync(ICallerContext caller, string receiptId,
        DateTime failedAt, string reason, CancellationToken cancellationToken)
    {
        var retrieved = await _emailDeliveryRepository.FindByReceiptIdAsync(receiptId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Result.Ok;
        }

        var email = retrieved.Value.Value;
        var delivered = email.ConfirmDeliveryFailed(receiptId, failedAt, reason);
        if (delivered.IsFailure)
        {
            if (delivered.Error.Is(ErrorCode.RuleViolation))
            {
                return Result.Ok;
            }

            return delivered.Error;
        }

        var saved = await _emailDeliveryRepository.SaveAsync(email, false, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        email = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Email {Receipt} confirmed delivery failed for {For}",
            receiptId, email.Recipient.Value.EmailAddress.Address);

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllEmailsAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_emailMessageQueue,
            message => SendEmailInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all email messages");

        return Result.Ok;
    }
#endif

    public async Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesAsync(ICallerContext caller,
        DateTime? sinceUtc, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var sinceWhen = sinceUtc ?? DateTime.UtcNow.SubtractDays(14);
        var searched =
            await _emailDeliveryRepository.SearchAllDeliveriesAsync(sinceWhen, tags, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var deliveries = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All email deliveries since {Since} were fetched",
            sinceUtc.ToIso8601());

        return searchOptions.ApplyWithMetadata(
            deliveries.Select(delivery => delivery.ToDeliveredEmail()));
    }

    public async Task<Result<bool, Error>> SendEmailAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<EmailMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var sent = await SendEmailInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Sent email message: {Message}", messageAsJson);
        return true;
    }

    private async Task<Result<bool, Error>> SendEmailInternalAsync(ICallerContext caller, EmailMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Message.IsInvalidParameter(x => x.Exists(), nameof(EmailMessage.Message), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Email_MissingMessage);
        }

        var messageId = QueuedMessageId.Create(message.MessageId!);
        if (messageId.IsFailure)
        {
            return messageId.Error;
        }

        var retrieved = await _emailDeliveryRepository.FindByMessageIdAsync(messageId.Value, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subject = message.Message!.Subject;
        var body = message.Message!.HtmlBody;
        var recipientEmailAddress = EmailAddress.Create(message.Message!.ToEmailAddress!);
        if (recipientEmailAddress.IsFailure)
        {
            return recipientEmailAddress.Error;
        }

        var recipientName = message.Message!.ToDisplayName ?? string.Empty;
        var recipient = EmailRecipient.Create(recipientEmailAddress.Value, recipientName);
        if (recipient.IsFailure)
        {
            return recipient.Error;
        }

        var senderEmailAddress = EmailAddress.Create(message.Message!.FromEmailAddress!);
        if (senderEmailAddress.IsFailure)
        {
            return senderEmailAddress.Error;
        }

        var senderName = message.Message!.FromDisplayName ?? string.Empty;
        var sender = EmailRecipient.Create(senderEmailAddress.Value, senderName);
        if (sender.IsFailure)
        {
            return sender.Error;
        }

        var tags = message.Message!.Tags;

        EmailDeliveryRoot email;
        var found = retrieved.Value.HasValue;
        if (found)
        {
            email = retrieved.Value.Value;
        }
        else
        {
            var created = EmailDeliveryRoot.Create(_recorder, _idFactory, messageId.Value);
            if (created.IsFailure)
            {
                return created.Error;
            }

            email = created.Value;

            var detailed = email.SetEmailDetails(subject, body, recipient.Value, tags);
            if (detailed.IsFailure)
            {
                return detailed.Error;
            }
        }

        var makeAttempt = email.AttemptSending();
        if (makeAttempt.IsFailure)
        {
            return makeAttempt.Error;
        }

        var isAlreadyDelivered = makeAttempt.Value;
        if (isAlreadyDelivered)
        {
            _recorder.TraceInformation(caller.ToCall(), "Email for {For} is already sent",
                email.Recipient.Value.EmailAddress.Address);
            return true;
        }

        var saved = await _emailDeliveryRepository.SaveAsync(email, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        email = saved.Value;
        var sent = await _emailDeliveryService.SendAsync(caller, subject!, body!, recipient.Value.EmailAddress,
            recipient.Value.DisplayName, sender.Value.EmailAddress,
            sender.Value.DisplayName, cancellationToken);
        if (sent.IsFailure)
        {
            var failed = email.FailedSending();
            if (failed.IsFailure)
            {
                return failed.Error;
            }

            var savedFailure = await _emailDeliveryRepository.SaveAsync(email, false, cancellationToken);
            if (savedFailure.IsFailure)
            {
                return savedFailure.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Sending of email for delivery for {For}, failed",
                savedFailure.Value.Recipient.Value.EmailAddress.Address);

            return sent.Error;
        }

        var succeeded = email.SucceededSending(sent.Value.ReceiptId.ToOptional());
        if (succeeded.IsFailure)
        {
            return succeeded.Error;
        }

        var updated = await _emailDeliveryRepository.SaveAsync(email, false, cancellationToken);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        email = updated.Value;
        _recorder.TraceInformation(caller.ToCall(), "Sent email for delivery for {For}",
            email.Recipient.Value.EmailAddress.Address);

        return true;
    }
}

public static class AncillaryEmailingConversionExtensions
{
    public static DeliveredEmail ToDeliveredEmail(this EmailDelivery email)
    {
        return new DeliveredEmail
        {
            Attempts = email.Attempts.HasValue
                ? email.Attempts.Value.Attempts.ToList()
                : new List<DateTime>(),
            Body = email.Body,
            IsSent = email.Sent.HasValue,
            SentAt = email.Sent.HasValue
                ? email.Sent.Value
                : null,
            Subject = email.Subject,
            ToDisplayName = email.ToDisplayName,
            ToEmailAddress = email.ToEmailAddress,
            Id = email.Id,
            IsDelivered = email.Delivered.HasValue,
            DeliveredAt = email.Delivered.HasValue
                ? email.Delivered.Value
                : null,
            IsDeliveryFailed = email.DeliveryFailed.HasValue,
            FailedDeliveryAt = email.DeliveryFailed.HasValue
                ? email.DeliveryFailed.Value
                : null,
            FailedDeliveryReason = email.DeliveryFailedReason.HasValue
                ? email.DeliveryFailedReason.Value
                : null,
            Tags = email.Tags.HasValue
                ? email.Tags.Value.FromJson<List<string>>()!
                : new List<string>()
        };
    }
}