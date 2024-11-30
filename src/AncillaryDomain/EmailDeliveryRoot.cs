using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Events.Shared.Ancillary.EmailDelivery;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using Domain.Shared.Ancillary;

namespace AncillaryDomain;

public sealed class EmailDeliveryRoot : AggregateRootBase
{
    public static Result<EmailDeliveryRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        QueuedMessageId messageId)
    {
        var root = new EmailDeliveryRoot(recorder, idFactory);
        root.RaiseCreateEvent(AncillaryDomain.Events.EmailDelivery.Created(root.Id, messageId));
        return root;
    }

    private EmailDeliveryRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private EmailDeliveryRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public SendingAttempts Attempts { get; private set; } = SendingAttempts.Empty;

    public Optional<DateTime> Delivered { get; private set; } = Optional<DateTime>.None;

    public Optional<DateTime> FailedDelivery { get; private set; } = Optional<DateTime>.None;

    public bool IsAttempted => Attempts.HasBeenAttempted;

    public bool IsDelivered => Delivered.HasValue;

    public bool IsFailedDelivery => FailedDelivery.HasValue;

    public bool IsSent => Sent.HasValue;

    public QueuedMessageId MessageId { get; private set; } = QueuedMessageId.Empty;

    public Optional<EmailRecipient> Recipient { get; private set; } = Optional<EmailRecipient>.None;

    public Optional<DateTime> Sent { get; private set; } = Optional<DateTime>.None;

    public List<string> Tags { get; private set; } = [];

    public Optional<DeliveredEmailContentType> ContentType { get; private set; } =
        Optional<DeliveredEmailContentType>.None;

    public static AggregateRootFactory<EmailDeliveryRoot> Rehydrate()
    {
        return (identifier, container, _) => new EmailDeliveryRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                var messageId = QueuedMessageId.Create(created.MessageId);
                if (messageId.IsFailure)
                {
                    return messageId.Error;
                }

                MessageId = messageId.Value;
                return Result.Ok;
            }

            case EmailDetailsChanged changed:
            {
                var emailAddress = EmailAddress.Create(changed.ToEmailAddress);
                if (emailAddress.IsFailure)
                {
                    return emailAddress.Error;
                }

                var recipient = EmailRecipient.Create(emailAddress.Value, changed.ToDisplayName);
                if (recipient.IsFailure)
                {
                    return recipient.Error;
                }

                ContentType = changed.ContentType;
                Recipient = recipient.Value;
                Tags = changed.Tags;
                Recorder.TraceDebug(null, "EmailDelivery {Id} has updated the email details", Id);
                return Result.Ok;
            }

            case SendingAttempted changed:
            {
                var attempted = Attempts.Attempt(changed.When);
                if (attempted.IsFailure)
                {
                    return attempted.Error;
                }

                Attempts = attempted.Value;
                Recorder.TraceDebug(null, "EmailDelivery {Id} is attempting a delivery", Id);
                return Result.Ok;
            }

            case SendingFailed _:
            {
                Recorder.TraceDebug(null, "EmailDelivery {Id} failed a delivery", Id);
                return Result.Ok;
            }

            case SendingSucceeded changed:
            {
                Sent = changed.When;
                Delivered = Optional<DateTime>.None;
                Recorder.TraceDebug(null, "EmailDelivery {Id} succeeded sending", Id);
                return Result.Ok;
            }

            case DeliveryConfirmed confirmed:
            {
                Delivered = confirmed.When;
                FailedDelivery = Optional<DateTime>.None;
                Recorder.TraceDebug(null, "EmailDelivery {Id} confirmed delivery", Id);
                return Result.Ok;
            }

            case DeliveryFailureConfirmed confirmed:
            {
                Delivered = Optional<DateTime>.None;
                FailedDelivery = confirmed.When;
                Recorder.TraceDebug(null, "EmailDelivery {Id} confirmed failed delivery", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<bool, Error> AttemptSending()
    {
        if (IsSent)
        {
            return true;
        }

        var when = DateTime.UtcNow;
        var attempted = RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.SendingAttempted(Id, when));
        if (attempted.IsFailure)
        {
            return attempted.Error;
        }

        return false;
    }

    public Result<Error> ConfirmDelivery(string receiptId, DateTime when)
    {
        if (!IsSent)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotSent);
        }

        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadyDelivered);
        }

        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.DeliveryConfirmed(Id, receiptId, when));
    }

    public Result<Error> ConfirmDeliveryFailed(string receiptId, DateTime when, string reason)
    {
        if (!IsSent)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotSent);
        }

        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadyDelivered);
        }

        if (IsFailedDelivery)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.DeliveryFailureConfirmed(Id, receiptId, when, reason));
    }

    public Result<Error> FailedSending()
    {
        if (IsSent)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadySent);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.SendingFailed(Id, when));
    }

    public Result<Error> SetContent(string? subject, string? body, EmailRecipient recipient,
        IReadOnlyList<string>? tags)
    {
        if (subject.IsInvalidParameter(x => x.HasValue(), nameof(subject),
                Resources.EmailDeliveryRoot_HtmlEmail_MissingSubject, out var error1))
        {
            return error1;
        }

        if (body.IsInvalidParameter(x => x.HasValue(), nameof(body), Resources.EmailDeliveryRoot_HtmlEmail_MissingBody,
                out var error2))
        {
            return error2;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.EmailDetailsChanged(Id, subject!, body!, Optional<string>.None,
                Optional<Dictionary<string, string>>.None, recipient, tags));
    }

    public Result<Error> SetContent(string? templateId, string? subject, Dictionary<string, string>? substitutions,
        EmailRecipient recipient, IReadOnlyList<string>? tags)
    {
        if (templateId.IsInvalidParameter(x => x.HasValue(), nameof(templateId),
                Resources.EmailDeliveryRoot_TemplatedEmail_MissingTemplateId, out var error1))
        {
            return error1;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.EmailDetailsChanged(Id, subject, Optional<string>.None,
                templateId!, substitutions, recipient, tags));
    }

    public Result<Error> SucceededSending(Optional<string> receiptId)
    {
        if (IsSent)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadySent);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.SendingSucceeded(Id, receiptId, when));
    }

#if TESTINGONLY
    public void TestingOnly_DeliverEmail()
    {
        Sent = DateTime.UtcNow;
    }
#endif
}