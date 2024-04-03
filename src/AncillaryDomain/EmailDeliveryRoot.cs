using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Events.Shared.Ancillary.EmailDelivery;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;

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

    public DeliveryAttempts Attempts { get; private set; } = DeliveryAttempts.Empty;

    public Optional<DateTime> Delivered { get; private set; } = Optional<DateTime>.None;

    public bool IsAttempted => Attempts.HasBeenAttempted;

    public bool IsDelivered => Delivered.HasValue;

    public QueuedMessageId MessageId { get; private set; } = QueuedMessageId.Empty;

    public Optional<EmailRecipient> Recipient { get; private set; } = Optional<EmailRecipient>.None;

    public static AggregateRootFactory<EmailDeliveryRoot> Rehydrate()
    {
        return (identifier, container, _) => new EmailDeliveryRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
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
                if (!messageId.IsSuccessful)
                {
                    return messageId.Error;
                }

                MessageId = messageId.Value;
                return Result.Ok;
            }

            case EmailDetailsChanged changed:
            {
                var emailAddress = EmailAddress.Create(changed.ToEmailAddress);
                if (!emailAddress.IsSuccessful)
                {
                    return emailAddress.Error;
                }

                var recipient = EmailRecipient.Create(emailAddress.Value, changed.ToDisplayName);
                if (!recipient.IsSuccessful)
                {
                    return recipient.Error;
                }

                Recipient = recipient.Value;
                Recorder.TraceDebug(null, "EmailDelivery {Id} has updated the email details", Id);
                return Result.Ok;
            }

            case DeliveryAttempted changed:
            {
                var attempted = Attempts.Attempt(changed.When);
                if (!attempted.IsSuccessful)
                {
                    return attempted.Error;
                }

                Attempts = attempted.Value;
                Recorder.TraceDebug(null, "EmailDelivery {Id} is attempting a delivery", Id);
                return Result.Ok;
            }

            case DeliveryFailed _:
            {
                Recorder.TraceDebug(null, "EmailDelivery {Id} failed a delivery", Id);
                return Result.Ok;
            }

            case DeliverySucceeded changed:
            {
                Delivered = changed.When;
                Recorder.TraceDebug(null, "EmailDelivery {Id} succeeded delivery", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<bool, Error> AttemptDelivery()
    {
        if (IsDelivered)
        {
            return true;
        }

        var when = DateTime.UtcNow;
        var attempted = RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.DeliveryAttempted(Id, when));
        if (!attempted.IsSuccessful)
        {
            return attempted.Error;
        }

        return false;
    }

    public Result<Error> FailedDelivery()
    {
        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadyDelivered);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.DeliveryFailed(Id, when));
    }

    public Result<Error> SetEmailDetails(string? subject, string? body, EmailRecipient recipient)
    {
        if (subject.IsInvalidParameter(x => x.HasValue(), nameof(subject),
                Resources.EmailDeliveryRoot_MissingEmailSubject, out var error1))
        {
            return error1;
        }

        if (body.IsInvalidParameter(x => x.HasValue(), nameof(body), Resources.EmailDeliveryRoot_MissingEmailBody,
                out var error2))
        {
            return error2;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.EmailDetailsChanged(Id, subject!, body!, recipient));
    }

    public Result<Error> SucceededDelivery(Optional<string> transactionId)
    {
        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_AlreadyDelivered);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.EmailDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(
            AncillaryDomain.Events.EmailDelivery.DeliverySucceeded(Id, when));
    }

#if TESTINGONLY
    public void TestingOnly_DeliverEmail()
    {
        Delivered = DateTime.UtcNow;
    }
#endif
}