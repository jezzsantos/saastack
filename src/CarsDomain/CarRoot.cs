using CarsDomain.Events;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace CarsDomain;

public sealed class CarRoot : AggregateRootBase
{
    public static Result<CarRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var car = new CarRoot(recorder, idFactory);
        car.RaiseCreateEvent(Car.Created.Create(car.Id, organizationId));
        return car;
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory, Identifier identifier) : base(recorder,
        idFactory, identifier)
    {
    }

    public static AggregateRootFactory<CarRoot> Rehydrate()
    {
        return (identifier, container, _) => new CarRoot(container.Resolve<IRecorder>(),
            container.Resolve<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        var unavailabilityInvariants = Unavailabilities.EnsureInvariants();
        if (!unavailabilityInvariants.IsSuccessful)
        {
            return unavailabilityInvariants.Error;
        }

        if (Unavailabilities.Count > 0)
        {
            if (Manufacturer.NotExists())
            {
                return Error.RuleViolation(Resources.CarRoot_NotManufactured);
            }

            if (Owner.NotExists())
            {
                return Error.RuleViolation(Resources.CarRoot_NotOwned);
            }

            if (License.NotExists())
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
            case Car.Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                Status = created.Status.ToEnum<CarStatus>();
                return Result.Ok;
            }

            case Car.ManufacturerChanged changed:
            {
                var manufacturer = Manufacturer.Create(changed.Year, changed.Make, changed.Model);
                return manufacturer.Match(manu =>
                {
                    Manufacturer = manu.Value;
                    Recorder.TraceDebug(null, "Car {Id} changed manufacturer to {Year}, {Make}, {Model}", Id,
                        changed.Year, changed.Make, changed.Model);
                    return Result.Ok;
                }, error => error);
            }

            case Car.OwnershipChanged changed:
            {
                var owner = VehicleOwner.Create(changed.Owner);
                if (!owner.IsSuccessful)
                {
                    return owner.Error;
                }

                Owner = owner.Value;
                Managers = Managers.Append(changed.Owner.ToId());
                Recorder.TraceDebug(null, "Car {Id} changed ownership to {Owner}", Id, Owner);
                return Result.Ok;
            }

            case Car.RegistrationChanged changed:
            {
                var jurisdiction = Jurisdiction.Create(changed.Jurisdiction);
                if (!jurisdiction.IsSuccessful)
                {
                    return jurisdiction.Error;
                }

                var number = NumberPlate.Create(changed.Number);
                if (!number.IsSuccessful)
                {
                    return number.Error;
                }

                var plate = LicensePlate.Create(jurisdiction.Value, number.Value);
                if (!plate.IsSuccessful)
                {
                    return plate.Error;
                }

                License = plate.Value;
                Status = changed.Status.ToEnum<CarStatus>();
                Recorder.TraceDebug(null, "Car {Id} registration changed to {Jurisdiction}, {Number}", Id,
                    changed.Jurisdiction, changed.Number);
                return Result.Ok;
            }

            case Car.UnavailabilitySlotAdded created:
            {
                var unavailability = RaiseEventToChildEntity(isReconstituting, created, idFactory =>
                    UnavailabilityEntity.Create(Recorder, idFactory, RaiseChangeEvent), e => e.UnavailabilityId!);
                if (!unavailability.IsSuccessful)
                {
                    return unavailability.Error;
                }

                Unavailabilities.Add(unavailability.Value);
                Recorder.TraceDebug(null, "Car {Id} had been made unavailable from {From} until {To}, for {CausedBy}",
                    Id, created.From, created.To, created.CausedByReason);
                return Result.Ok;
            }

            case Car.UnavailabilitySlotRemoved deleted:
            {
                Unavailabilities.Remove(deleted.UnavailabilityId.ToId());
                Recorder.TraceDebug(null, "Car {Id} has had unavailability {UnavailabilityId} removed", Id,
                    deleted.RootId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public LicensePlate? License { get; private set; }

    public VehicleManagers Managers { get; private set; } = VehicleManagers.Create();

    public Manufacturer? Manufacturer { get; private set; }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public VehicleOwner? Owner { get; private set; }

    public CarStatus Status { get; private set; }

    public Unavailabilities Unavailabilities { get; } = new();

    public Result<Error> ChangeRegistration(LicensePlate plate)
    {
        return RaiseChangeEvent(Car.RegistrationChanged.Create(Id, OrganizationId, plate));
    }

    public Result<Error> ReleaseUnavailability(TimeSlot slot)
    {
        var unavailability = Unavailabilities.FindSlot(slot);
        if (unavailability.Exists())
        {
            return RaiseChangeEvent(
                Car.UnavailabilitySlotRemoved.Create(Id, OrganizationId, unavailability.Id));
        }

        return Result.Ok;
    }

    public Result<bool, Error> ReserveIfAvailable(TimeSlot slot, string referenceId)
    {
        if (slot.IsInvalidParameter(s => s.StartsAfter(DateTime.UtcNow), nameof(slot),
                Resources.CarRoot_ReserveInPast, out var error))
        {
            return error;
        }

        if (!IsAvailable(slot))
        {
            return false;
        }

        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Reservation, referenceId);
        if (!causedBy.IsSuccessful)
        {
            return causedBy.Error;
        }

        var raiseEvent = RaiseChangeEvent(Car.UnavailabilitySlotAdded.Create(Id, OrganizationId, slot, causedBy.Value));
        if (!raiseEvent.IsSuccessful)
        {
            return raiseEvent.Error;
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
        if (!causedBy.IsSuccessful)
        {
            return causedBy.Error;
        }

        return RaiseChangeEvent(Car.UnavailabilitySlotAdded.Create(Id, OrganizationId, slot,
            causedBy.Value));
    }

    public Result<Error> SetManufacturer(Manufacturer manufacturer)
    {
        return RaiseChangeEvent(Car.ManufacturerChanged.Create(Id, OrganizationId, manufacturer));
    }

    public Result<Error> SetOwnership(VehicleOwner owner)
    {
        return RaiseChangeEvent(Car.OwnershipChanged.Create(Id, OrganizationId, owner));
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
        if (!causedBy.IsSuccessful)
        {
            return causedBy.Error;
        }

        return RaiseChangeEvent(Car.UnavailabilitySlotAdded.Create(Id, OrganizationId, slot, causedBy.Value));
    }

    private bool IsAvailable(TimeSlot slot)
    {
        return !Unavailabilities.Any(una => una.Overlaps(slot).Match(optional => optional.Value, _ => false));
    }

#if TESTINGONLY
    public Result<Error> TestingOnly_AddUnavailability(TimeSlot slot, CausedBy causedBy)
    {
        return RaiseChangeEvent(Car.UnavailabilitySlotAdded.Create(Id, OrganizationId, slot,
            causedBy));
    }

    public void TestingOnly_ResetDetails(Manufacturer? manufacturer, VehicleOwner? owner, LicensePlate? plate)
    {
        Manufacturer = manufacturer;
        Owner = owner;
        License = plate;
    }
#endif
}