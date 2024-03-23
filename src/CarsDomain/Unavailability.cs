using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace CarsDomain;

public sealed class Unavailability : EntityBase
{
    public static Result<Unavailability, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler)
    {
        return new Unavailability(recorder, idFactory, rootEventHandler);
    }

    private Unavailability(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    public Identifier? CarId { get; private set; }

    public CausedBy? CausedBy { get; private set; }

    public Identifier? OrganizationId { get; private set; }

    public TimeSlot? Slot { get; private set; }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (CarId.NotExists() && Slot.NotExists() && CausedBy.NotExists())
        {
            return Error.RuleViolation(Resources.UnavailabilityEntity_NotAssigned);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case Events.UnavailabilitySlotAdded added:
            {
                var slot = TimeSlot.Create(added.From, added.To);
                if (!slot.IsSuccessful)
                {
                    return slot.Error;
                }

                var causedBy = CausedBy.Create(added.CausedByReason, added.CausedByReference);
                if (!causedBy.IsSuccessful)
                {
                    return causedBy.Error;
                }

                OrganizationId = added.OrganizationId.ToId();
                CarId = added.RootId.ToId();
                Slot = slot.Value;
                CausedBy = causedBy.Value;
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public bool IsDifferentCause(Unavailability unavailability)
    {
        if (CausedBy.NotExists())
        {
            return unavailability.CausedBy.Exists();
        }

        if (unavailability.CausedBy.NotExists())
        {
            return CausedBy.Exists();
        }

        if ((CausedBy.Reference.Exists() || unavailability.CausedBy.Reference.Exists())
            && CausedBy.Reference != unavailability.CausedBy.Reference)
        {
            return true;
        }

        return CausedBy != unavailability.CausedBy;
    }

    public Result<bool, Error> Overlaps(TimeSlot slot)
    {
        if (Slot.Exists())
        {
            return Slot.IsOverlapping(slot);
        }

        return Error.RuleViolation(Resources.UnavailabilityEntity_NotAssigned);
    }

#if TESTINGONLY
    public void TestingOnly_Assign(Identifier carId, Identifier organizationId, TimeSlot timeSlot,
        CausedBy causedBy)
    {
        RaiseChangeEvent(Events.UnavailabilitySlotAdded.Create(carId, organizationId, timeSlot, causedBy));
    }
#endif
}