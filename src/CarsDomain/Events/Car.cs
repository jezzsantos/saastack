using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace CarsDomain.Events;

public static class Car
{
    public class Created : IDomainEvent
    {
        public static Created Create(Identifier id, Identifier organizationId)
        {
            return new Created
            {
                RootId = id,
                OrganizationId = organizationId,
                OccurredUtc = DateTime.UtcNow,
                Status = CarStatus.Unregistered.ToString()
            };
        }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required string OrganizationId { get; set; }

        public required string Status { get; set; }
    }

    public class ManufacturerChanged : IDomainEvent
    {
        public static ManufacturerChanged Create(Identifier id, Identifier organizationId,
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

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required string Make { get; set; }

        public required string Model { get; set; }

        public required string OrganizationId { get; set; }

        public required int Year { get; set; }
    }

    public class OwnershipChanged : IDomainEvent
    {
        public static OwnershipChanged Create(Identifier id, Identifier organisationId, VehicleOwner owner)
        {
            return new OwnershipChanged
            {
                RootId = id,
                OrganisationId = organisationId,
                Owner = owner.OwnerId,
                Managers = new List<string> { owner.OwnerId },
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required List<string> Managers { get; set; }

        public required string OrganisationId { get; set; }

        public required string Owner { get; set; }
    }

    public class RegistrationChanged : IDomainEvent
    {
        public static RegistrationChanged Create(Identifier id, Identifier organisationId, LicensePlate plate)
        {
            return new RegistrationChanged
            {
                RootId = id,
                OrganisationId = organisationId,
                Jurisdiction = plate.Jurisdiction,
                Number = plate.Number,
                Status = CarStatus.Registered.ToString(),
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required string Jurisdiction { get; set; }

        public required string Number { get; set; }

        public required string OrganisationId { get; set; }

        public required string Status { get; set; }
    }

    public class UnavailabilitySlotAdded : IDomainEvent
    {
        public static UnavailabilitySlotAdded Create(Identifier id, Identifier organizationId, TimeSlot slot,
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

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required UnavailabilityCausedBy CausedByReason { get; set; }

        public string? CausedByReference { get; set; }

        public required DateTime From { get; set; }

        public required string OrganizationId { get; set; }

        public required DateTime To { get; set; }

        public string? UnavailabilityId { get; set; }
    }

    public class UnavailabilitySlotRemoved : IDomainEvent
    {
        public static UnavailabilitySlotRemoved Create(Identifier id, Identifier organizationId,
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

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required string OrganizationId { get; set; }

        public required string UnavailabilityId { get; set; }
    }
}