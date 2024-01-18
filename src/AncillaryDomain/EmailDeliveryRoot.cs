using AncillaryDomain;
using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

public sealed class EmailDeliveryRoot : AggregateRootBase
{
    public static Result<EmailDeliveryRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        string messageId)
    {
        var root = new EmailDeliveryRoot(recorder, idFactory);
        root.RaiseCreateEvent(AncillaryDomain.Events.EmailDelivery.Created.Create(root.Id, messageId));
        return root;
    }

    private EmailDeliveryRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    public bool IsDelivered { get; private set; } = false;

    public string MessageId { get; private set; } = Identifier.Empty();

    public DeliveryTimeLine TimeLine { get; private set; }

    public string TransactionId { get; private set; } = string.Empty;
    
    public EmailAddress EmailTo { get; private set; }

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
            case Events.EmailDelivery.Created created:
            {
                MessageId = created.MessageId;
                return Result.Ok;
            }

            case Events.EmailDelivery.Attempted changed:
            {
                var attempted = TimeLine.Attempt(changed.When);
                if (!attempted.IsSuccessful)
                {
                    return attempted.Error;
                }

                TimeLine = attempted.Value;
                return Result.Ok;
            }

            case Events.EmailDelivery.CompletedDelivery changed:
            {
                IsDelivered = true;
                TransactionId = changed.TransactionId;
                return Result.Ok;
            }

            case AncillaryDomain.Events.EmailDelivery.RecipientAdded changed:
            {
                var created = EmailAddress.Create(changed.To);
                if (!created.IsSuccessful)
                {
                    return created.Error;
                }

                EmailTo = created.Value;
                return Result.Ok;
            }
                
                

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AttemptDelivery()
    {
        var when = DateTime.UtcNow;
        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.Attempted.Create(Id, when));
    }

    public Result<Error> CompleteDelivery(Optional<string> transactionId)
    {
        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.CompletedDelivery.Create(Id, transactionId));
    }

    public Result<Error> SetupRecipients(EmailAddress to)
    {
        return RaiseChangeEvent(AncillaryDomain.Events.EmailDelivery.RecipientAdded.Create(Id, to));
    }
}