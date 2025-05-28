using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Ancillary.SmsDelivery;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;

namespace AncillaryDomain;

public sealed class SmsDeliveryRoot : AggregateRootBase
{
    public static Result<SmsDeliveryRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        QueuedMessageId messageId, Optional<Identifier> organizationId, DatacenterLocation hostRegion)
    {
        var root = new SmsDeliveryRoot(recorder, idFactory);
        root.RaiseCreateEvent(
            AncillaryDomain.Events.SmsDelivery.Created(root.Id, messageId, organizationId, hostRegion));
        return root;
    }

    private SmsDeliveryRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private SmsDeliveryRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public SendingAttempts Attempts { get; private set; } = SendingAttempts.Empty;

    public Optional<DateTime> Delivered { get; private set; } = Optional<DateTime>.None;

    public Optional<DateTime> FailedDelivery { get; private set; } = Optional<DateTime>.None;

    public Optional<Identifier> OrganizationId { get; private set; }

    public bool IsAttempted => Attempts.HasBeenAttempted;

    public bool IsDelivered => Delivered.HasValue;

    public bool IsFailedDelivery => FailedDelivery.HasValue;

    public bool IsSent => Sent.HasValue;

    public QueuedMessageId MessageId { get; private set; } = QueuedMessageId.Empty;

    public Optional<PhoneNumber> Recipient { get; private set; } = Optional<PhoneNumber>.None;

    public Optional<DateTime> Sent { get; private set; } = Optional<DateTime>.None;

    public List<string> Tags { get; private set; } = [];

    public DatacenterLocation HostRegion { get; private set; } = DatacenterLocations.Unknown;

    [UsedImplicitly]
    public static AggregateRootFactory<SmsDeliveryRoot> Rehydrate()
    {
        return (identifier, container, _) => new SmsDeliveryRoot(container.GetRequiredService<IRecorder>(),
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
                OrganizationId = created.OrganizationId.HasValue()
                    ? created.OrganizationId.ToId()
                    : Optional<Identifier>.None;
                HostRegion = DatacenterLocations.FindOrDefault(created.HostRegion);
                return Result.Ok;
            }

            case SmsDetailsChanged changed:
            {
                var recipient = PhoneNumber.Create(changed.ToPhoneNumber);
                if (recipient.IsFailure)
                {
                    return recipient.Error;
                }

                Recipient = recipient.Value;
                Tags = changed.Tags;
                Recorder.TraceDebug(null, "SmsDelivery {Id} has updated the sms details", Id);
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
                Recorder.TraceDebug(null, "SmsDelivery {Id} is attempting a delivery", Id);
                return Result.Ok;
            }

            case SendingFailed _:
            {
                Recorder.TraceDebug(null, "SmsDelivery {Id} failed a delivery", Id);
                return Result.Ok;
            }

            case SendingSucceeded changed:
            {
                Sent = changed.When;
                Delivered = Optional<DateTime>.None;
                Recorder.TraceDebug(null, "SmsDelivery {Id} succeeded sending", Id);
                return Result.Ok;
            }

            case DeliveryConfirmed confirmed:
            {
                Delivered = confirmed.When;
                FailedDelivery = Optional<DateTime>.None;
                Recorder.TraceDebug(null, "SmsDelivery {Id} confirmed delivery", Id);
                return Result.Ok;
            }

            case DeliveryFailureConfirmed confirmed:
            {
                Delivered = Optional<DateTime>.None;
                FailedDelivery = confirmed.When;
                Recorder.TraceDebug(null, "SmsDelivery {Id} confirmed failed delivery", Id);
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
        var attempted = RaiseChangeEvent(AncillaryDomain.Events.SmsDelivery.SendingAttempted(Id, when));
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
            return Error.RuleViolation(Resources.SmsDeliveryRoot_NotSent);
        }

        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_AlreadyDelivered);
        }

        return RaiseChangeEvent(AncillaryDomain.Events.SmsDelivery.DeliveryConfirmed(Id, receiptId, when));
    }

    public Result<Error> ConfirmDeliveryFailed(string receiptId, DateTime when, string reason)
    {
        if (!IsSent)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_NotSent);
        }

        if (IsDelivered)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_AlreadyDelivered);
        }

        if (IsFailedDelivery)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.SmsDelivery.DeliveryFailureConfirmed(Id, receiptId, when, reason));
    }

    public Result<Error> FailedSending()
    {
        if (IsSent)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_AlreadySent);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(AncillaryDomain.Events.SmsDelivery.SendingFailed(Id, when));
    }

    public Result<Error> SetSmsDetails(string? body, PhoneNumber recipient,
        IReadOnlyList<string>? tags)
    {
        if (body.IsInvalidParameter(x => x.HasValue(), nameof(body), Resources.SmsDeliveryRoot_MissingSmsBody,
                out var error2))
        {
            return error2;
        }

        return RaiseChangeEvent(
            AncillaryDomain.Events.SmsDelivery.SmsDetailsChanged(Id, body!, recipient, tags));
    }

    public Result<Error> SucceededSending(Optional<string> receiptId)
    {
        if (IsSent)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_AlreadySent);
        }

        if (!IsAttempted)
        {
            return Error.RuleViolation(Resources.SmsDeliveryRoot_NotAttempted);
        }

        var when = DateTime.UtcNow;
        return RaiseChangeEvent(AncillaryDomain.Events.SmsDelivery.SendingSucceeded(Id, receiptId, when));
    }

#if TESTINGONLY
    public void TestingOnly_DeliverSms()
    {
        Sent = DateTime.UtcNow;
    }
#endif
}