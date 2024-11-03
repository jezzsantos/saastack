using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Cars;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared.Cars;

namespace CarsDomain;

public sealed class CarRoot : AggregateRootBase
{
    public static Result<CarRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new CarRoot(recorder, idFactory);
        root.RaiseCreateEvent(CarsDomain.Events.Created(root.Id, organizationId));
        return root;
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Optional<LicensePlate> License { get; private set; }

    public VehicleManagers Managers { get; private set; } = VehicleManagers.Empty;

    public Optional<Manufacturer> Manufacturer { get; private set; }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public Optional<VehicleOwner> Owner { get; private set; }

    public CarStatus Status { get; private set; }

    public Unavailabilities Unavailabilities { get; } = new();

    public static AggregateRootFactory<CarRoot> Rehydrate()
    {
        return (identifier, container, _) => new CarRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        var unavailabilityInvariants = Unavailabilities.EnsureInvariants();
        if (unavailabilityInvariants.IsFailure)
        {
            return unavailabilityInvariants.Error;
        }

        if (Unavailabilities.Count > 0)
        {
            if (!Manufacturer.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotManufactured);
            }

            if (!Owner.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotOwned);
            }

            if (!License.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotRegistered);
            }
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                Status = created.Status;
                return Result.Ok;
            }

            case ManufacturerChanged changed:
            {
                var manufacturer = CarsDomain.Manufacturer.Create(changed.Year, changed.Make, changed.Model);
                return manufacturer.Match(manu =>
                {
                    Manufacturer = manu.Value;
                    Recorder.TraceDebug(null, "Car {Id} changed manufacturer to {Year}, {Make}, {Model}", Id,
                        changed.Year, changed.Make, changed.Model);
                    return Result.Ok;
                }, error => error);
            }

            case OwnershipChanged changed:
            {
                var owner = VehicleOwner.Create(changed.Owner);
                if (owner.IsFailure)
                {
                    return owner.Error;
                }

                Owner = owner.Value;
                Managers = Managers.Append(changed.Owner.ToId());
                Recorder.TraceDebug(null, "Car {Id} changed ownership to {Owner}", Id, Owner);
                return Result.Ok;
            }

            case RegistrationChanged changed:
            {
                var jurisdiction = Jurisdiction.Create(changed.Jurisdiction);
                if (jurisdiction.IsFailure)
                {
                    return jurisdiction.Error;
                }

                var number = NumberPlate.Create(changed.Number);
                if (number.IsFailure)
                {
                    return number.Error;
                }

                var plate = LicensePlate.Create(jurisdiction.Value, number.Value);
                if (plate.IsFailure)
                {
                    return plate.Error;
                }

                License = plate.Value;
                Status = changed.Status;
                Recorder.TraceDebug(null, "Car {Id} registration changed to {Jurisdiction}, {Number}", Id,
                    changed.Jurisdiction, changed.Number);
                return Result.Ok;
            }

            case UnavailabilitySlotAdded created:
            {
                var unavailability = RaiseEventToChildEntity(isReconstituting, created, idFactory =>
                    Unavailability.Create(Recorder, idFactory, RaiseChangeEvent), e => e.UnavailabilityId!);
                if (unavailability.IsFailure)
                {
                    return unavailability.Error;
                }

                Unavailabilities.Add(unavailability.Value);
                Recorder.TraceDebug(null, "Car {Id} had been made unavailable from {From} until {To}, for {CausedBy}",
                    Id, created.From, created.To, created.CausedByReason);
                return Result.Ok;
            }

            case UnavailabilitySlotRemoved deleted:
            {
                Unavailabilities.Remove(deleted.UnavailabilityId.ToId());
                Recorder.TraceDebug(null, "Car {Id} has had unavailability {UnavailabilityId} removed", Id,
                    deleted.UnavailabilityId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeRegistration(LicensePlate plate)
    {
        return RaiseChangeEvent(CarsDomain.Events.RegistrationChanged(Id, OrganizationId, plate));
    }

    public Result<Error> Delete(Identifier deleterId)
    {
        if (!Owner.HasValue)
        {
            return Error.RuleViolation(Resources.CarRoot_NotOwned);
        }

        if (deleterId != Owner.Value.OwnerId)
        {
            return Error.RuleViolation(Resources.CarRoot_NotDeletedByOwner);
        }

        return RaisePermanentDeleteEvent(CarsDomain.Events.Deleted(Id, deleterId));
    }

    public Result<Error> ReleaseUnavailability(TimeSlot slot)
    {
        var unavailability = Unavailabilities.FindSlot(slot);
        if (unavailability.Exists())
        {
            return RaiseChangeEvent(
                CarsDomain.Events.UnavailabilitySlotRemoved(Id, OrganizationId, unavailability.Id));
        }

        return Result.Ok;
    }

    public Result<bool, Error> ReserveIfAvailable(TimeSlot slot, Optional<string> referenceId)
    {
        if (slot.IsInvalidParameter(s => s.StartsAfter(DateTime.UtcNow), nameof(slot),
                Resources.CarRoot_ReserveInPast, out var error1))
        {
            return error1;
        }

        if (referenceId.IsInvalidParameter(r => r.HasValue, nameof(referenceId),
                Resources.CarRoot_ReferenceMissing, out var error2))
        {
            return error2;
        }

        if (!IsAvailable(slot))
        {
            return false;
        }

        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Reservation, referenceId);
        if (causedBy.IsFailure)
        {
            return causedBy.Error;
        }

        var raised =
            RaiseChangeEvent(
                CarsDomain.Events.UnavailabilitySlotAdded(Id, OrganizationId, slot, causedBy.Value));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        return true;
    }

    public Result<Error> ScheduleMaintenance(TimeSlot slot)
    {
        if (slot.IsInvalidParameter(
                s => s.StartsAfter(DateTime.UtcNow.Add(Validations.Car.MinScheduledMaintenanceLeadTime)), nameof(slot),
                Resources.CarRoot_ScheduleMaintenanceLessThanMinimumLeadTime.Format(Validations.Car
                    .MinScheduledMaintenanceLeadTime.TotalHours), out var error))
        {
            return error;
        }

        if (!IsAvailable(slot))
        {
            return Error.RuleViolation(Resources.CarRoot_Unavailable);
        }

        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Maintenance, null);
        if (causedBy.IsFailure)
        {
            return causedBy.Error;
        }

        return RaiseChangeEvent(CarsDomain.Events.UnavailabilitySlotAdded(Id, OrganizationId, slot,
            causedBy.Value));
    }

    public Result<Error> SetManufacturer(Manufacturer manufacturer)
    {
        return RaiseChangeEvent(CarsDomain.Events.ManufacturerChanged(Id, OrganizationId, manufacturer));
    }

    public Result<Error> SetOwnership(VehicleOwner owner)
    {
        return RaiseChangeEvent(CarsDomain.Events.OwnershipChanged(Id, OrganizationId, owner));
    }

    public Result<Error> TakeOffline(TimeSlot slot)
    {
        if (slot.IsInvalidParameter(s => s.StartsAfter(DateTime.UtcNow), nameof(slot),
                Resources.CarRoot_OfflineInPast, out var error))
        {
            return error;
        }

        if (!IsAvailable(slot))
        {
            return Error.RuleViolation(Resources.CarRoot_Unavailable);
        }

        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Offline, null);
        if (causedBy.IsFailure)
        {
            return causedBy.Error;
        }

        return RaiseChangeEvent(
            CarsDomain.Events.UnavailabilitySlotAdded(Id, OrganizationId, slot, causedBy.Value));
    }

    private bool IsAvailable(TimeSlot slot)
    {
        return !Unavailabilities.Any(una => una.Overlaps(slot).Match(optional => optional.Value, _ => false));
    }

#if TESTINGONLY
    public Result<Error> TestingOnly_AddUnavailability(TimeSlot slot, CausedBy causedBy)
    {
        return RaiseChangeEvent(CarsDomain.Events.UnavailabilitySlotAdded(Id, OrganizationId, slot,
            causedBy));
    }

    public void TestingOnly_ResetDetails(Optional<Manufacturer> manufacturer, Optional<VehicleOwner> owner,
        Optional<LicensePlate> plate)
    {
        Manufacturer = manufacturer;
        Owner = owner;
        License = plate;
    }
#endif
}