using Domain.Common.ValueObjects;
using Domain.Events.Shared.Cars;
using Domain.Shared.Cars;

namespace CarsDomain;

public static class Events
{
    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created(id)
        {
            OrganizationId = organizationId,
            Status = CarStatus.Unregistered
        };
    }

    public static ManufacturerChanged ManufacturerChanged(Identifier id, Identifier organizationId,
        Manufacturer manufacturer)
    {
        return new ManufacturerChanged(id)
        {
            OrganizationId = organizationId,
            Year = manufacturer.Year,
            Make = manufacturer.Make,
            Model = manufacturer.Model
        };
    }

    public static OwnershipChanged OwnershipChanged(Identifier id, Identifier organizationId, VehicleOwner owner)
    {
        return new OwnershipChanged(id)
        {
            OrganizationId = organizationId,
            Owner = owner.OwnerId,
            Managers = [owner.OwnerId]
        };
    }

    public static RegistrationChanged RegistrationChanged(Identifier id, Identifier organizationId, LicensePlate plate)
    {
        return new RegistrationChanged(id)
        {
            OrganizationId = organizationId,
            Jurisdiction = plate.Jurisdiction,
            Number = plate.Number,
            Status = CarStatus.Registered
        };
    }

    public static UnavailabilitySlotAdded UnavailabilitySlotAdded(Identifier id, Identifier organizationId,
        TimeSlot slot,
        CausedBy causedBy)
    {
        return new UnavailabilitySlotAdded(id)
        {
            OrganizationId = organizationId,
            From = slot.From,
            To = slot.To,
            CausedByReason = causedBy.Reason,
            CausedByReference = causedBy.Reference,
            UnavailabilityId = null
        };
    }

    public static UnavailabilitySlotRemoved UnavailabilitySlotRemoved(Identifier id, Identifier organizationId,
        Identifier unavailabilityId)
    {
        return new UnavailabilitySlotRemoved(id)
        {
            OrganizationId = organizationId,
            UnavailabilityId = unavailabilityId
        };
    }
}