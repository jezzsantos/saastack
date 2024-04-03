using Domain.Common.ValueObjects;
using Domain.Events.Shared.Cars;

namespace CarsDomain;

public static class Events
{
    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created
        {
            RootId = id,
            OrganizationId = organizationId,
            OccurredUtc = DateTime.UtcNow,
            Status = CarStatus.Unregistered.ToString()
        };
    }

    public static ManufacturerChanged ManufacturerChanged(Identifier id, Identifier organizationId,
        Manufacturer manufacturer)
    {
        return new ManufacturerChanged
        {
            RootId = id,
            OrganizationId = organizationId,
            Year = manufacturer.Year,
            Make = manufacturer.Make,
            Model = manufacturer.Model,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static OwnershipChanged OwnershipChanged(Identifier id, Identifier organizationId, VehicleOwner owner)
    {
        return new OwnershipChanged
        {
            RootId = id,
            OrganizationId = organizationId,
            Owner = owner.OwnerId,
            Managers = new List<string> { owner.OwnerId },
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static RegistrationChanged RegistrationChanged(Identifier id, Identifier organizationId, LicensePlate plate)
    {
        return new RegistrationChanged
        {
            RootId = id,
            OrganizationId = organizationId,
            Jurisdiction = plate.Jurisdiction,
            Number = plate.Number,
            Status = CarStatus.Registered.ToString(),
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static UnavailabilitySlotAdded UnavailabilitySlotAdded(Identifier id, Identifier organizationId,
        TimeSlot slot,
        CausedBy causedBy)
    {
        return new UnavailabilitySlotAdded
        {
            RootId = id,
            OrganizationId = organizationId,
            From = slot.From,
            To = slot.To,
            CausedByReason = causedBy.Reason,
            CausedByReference = causedBy.Reference,
            UnavailabilityId = null,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static UnavailabilitySlotRemoved UnavailabilitySlotRemoved(Identifier id, Identifier organizationId,
        Identifier unavailabilityId)
    {
        return new UnavailabilitySlotRemoved
        {
            RootId = id,
            OrganizationId = organizationId,
            UnavailabilityId = unavailabilityId,
            OccurredUtc = DateTime.UtcNow
        };
    }
}