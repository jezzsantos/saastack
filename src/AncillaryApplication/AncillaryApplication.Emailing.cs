using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Shared;
using Application.Persistence.Shared.Extensions;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
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
        await _emailMessageQueue.DrainAllQueuedMessagesAsync(
            message => SendEmailInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all email messages");

        return Result.Ok;
    }
#endif

    public async Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesAsync(ICallerContext caller,
        DateTime? sinceUtc, string? organizationId, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var sinceWhen = sinceUtc ?? DateTime.UtcNow.SubtractDays(14);
        var searched =
            await _emailDeliveryRepository.SearchAllAsync(sinceWhen, organizationId, tags, searchOptions,
                cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var deliveries = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All email deliveries since {Since} were fetched",
            sinceUtc.ToIso8601());

        return deliveries.ToSearchResults(searchOptions, delivery => delivery.ToDeliveredEmail());
    }

    public async Task<Result<bool, Error>> SendEmailAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = messageAsJson.RehydrateQueuedMessage<EmailMessage>();
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
        if (message.Html.NotExists() && message.Template.NotExists())
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

        var toEmailAddress = message.Html.Exists()
            ? message.Html!.ToEmailAddress!
            : message.Template!.ToEmailAddress!;
        var recipientEmailAddress = EmailAddress.Create(toEmailAddress);
        if (recipientEmailAddress.IsFailure)
        {
            return recipientEmailAddress.Error;
        }

        var toDisplayName = (message.Html.Exists()
            ? message.Html!.ToDisplayName
            : message.Template!.ToDisplayName) ?? string.Empty;
        var recipient = EmailRecipient.Create(recipientEmailAddress.Value, toDisplayName);
        if (recipient.IsFailure)
        {
            return recipient.Error;
        }

        var fromEmailAddress = message.Html.Exists()
            ? message.Html!.FromEmailAddress!
            : message.Template!.FromEmailAddress!;
        var senderEmailAddress = EmailAddress.Create(fromEmailAddress);
        if (senderEmailAddress.IsFailure)
        {
            return senderEmailAddress.Error;
        }

        var fromDisplayName = (message.Html.Exists()
            ? message.Html!.FromDisplayName
            : message.Template!.FromDisplayName) ?? string.Empty;
        var sender = EmailRecipient.Create(senderEmailAddress.Value, fromDisplayName);
        if (sender.IsFailure)
        {
            return sender.Error;
        }

        EmailDeliveryRoot email;
        var found = retrieved.Value.HasValue;
        if (found)
        {
            email = retrieved.Value.Value;
        }
        else
        {
            var created = EmailDeliveryRoot.Create(_recorder, _idFactory, messageId.Value, message.TenantId.HasValue()
                ? message.TenantId.ToId()
                : Optional<Identifier>.None, caller.HostRegion);
            if (created.IsFailure)
            {
                return created.Error;
            }

            email = created.Value;

            if (message.Html.Exists())
            {
                var subject = message.Html!.Subject;
                var body = message.Html!.Body;
                var tags = message.Html.Tags.Exists()
                    ? message.Html!.Tags
                    : null;
                var detailed = email.SetContent(subject, body, recipient.Value, tags);
                if (detailed.IsFailure)
                {
                    return detailed.Error;
                }
            }

            if (message.Template.Exists())
            {
                var templateId = message.Template!.TemplateId;
                var subject = message.Template!.Subject;
                var tags = message.Template.Tags.Exists()
                    ? message.Template!.Tags
                    : null;
                var substitutions = message.Template!.Substitutions;
                var detailed = email.SetContent(templateId, subject, substitutions, recipient.Value, tags);
                if (detailed.IsFailure)
                {
                    return detailed.Error;
                }
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
            _recorder.TraceInformation(caller.ToCall(), "Email {Id} for {For} (from {Region}) is already sent",
                email.Id, email.Recipient.Value.EmailAddress.Address,
                message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);
            return true;
        }

        var saved = await _emailDeliveryRepository.SaveAsync(email, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        email = saved.Value;
        var sent = new Result<EmailDeliveryReceipt, Error>(Error.Unexpected());
        if (message.Html.Exists())
        {
            var subject = message.Html.Subject;
            var body = message.Html.Body;
            var tags = message.Html.Tags;
            sent = await _emailDeliveryService.SendHtmlAsync(caller, subject!, body!, recipient.Value.EmailAddress,
                recipient.Value.DisplayName, sender.Value.EmailAddress,
                sender.Value.DisplayName, tags, cancellationToken);
        }

        if (message.Template.Exists())
        {
            var templateId = message.Template.TemplateId!;
            var subject = message.Template.Subject;
            var tags = message.Template.Tags;
            var substitutions = message.Template.Substitutions!;
            sent = await _emailDeliveryService.SendTemplatedAsync(caller, templateId, subject, substitutions,
                recipient.Value.EmailAddress, recipient.Value.DisplayName, sender.Value.EmailAddress,
                sender.Value.DisplayName, tags, cancellationToken);
        }

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

            _recorder.TraceInformation(caller.ToCall(),
                "Sending of email {Id} for delivery for {For} (from {Region}), failed",
                email.Id, savedFailure.Value.Recipient.Value.EmailAddress.Address,
                message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

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
        _recorder.TraceInformation(caller.ToCall(), "Sent email {Id} for delivery for {For} (from {Region})",
            email.Id, email.Recipient.Value.EmailAddress.Address,
            message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

        return true;
    }
}

public static class AncillaryEmailingConversionExtensions
{
    public static DeliveredEmail ToDeliveredEmail(this EmailDelivery email)
    {
        return new DeliveredEmail
        {
            Created = email.Created.ToNullable<DateTime?, DateTime>(x => x!.Value) ?? DateTime.UtcNow,
            Attempts = email.Attempts.ToNullable(att => att.Attempts.ToList()) ?? [],
            Body = email.Body,
            IsSent = email.Sent.HasValue,
            SentAt = email.Sent.ToNullable<DateTime?, DateTime>(),
            Subject = email.Subject,
            ToDisplayName = email.ToDisplayName,
            ToEmailAddress = email.ToEmailAddress,
            Id = email.Id,
            OrganizationId = email.OrganizationId,
            IsDelivered = email.Delivered.HasValue,
            DeliveredAt = email.Delivered.ToNullable<DateTime?, DateTime>(),
            IsDeliveryFailed = email.DeliveryFailed.HasValue,
            FailedDeliveryAt = email.DeliveryFailed.ToNullable<DateTime?, DateTime>(),
            FailedDeliveryReason = email.DeliveryFailedReason.ToNullable(),
            Tags = email.Tags.ToNullable(tags => tags.FromJson<List<string>>()!) ?? []
        };
    }
}