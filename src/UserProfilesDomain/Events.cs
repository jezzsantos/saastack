using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
using PhoneNumber = Domain.Shared.PhoneNumber;

namespace UserProfilesDomain;

public static class Events
{
    public sealed class Created : IDomainEvent
    {
        public static Created Create(Identifier id, ProfileType type, Identifier userId, PersonName name)
        {
            return new Created
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault!,
                DisplayName = name.FirstName,
                Type = type.ToString()
            };
        }

        public required string DisplayName { get; set; }

        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string Type { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class EmailAddressChanged : IDomainEvent
    {
        public static EmailAddressChanged Create(Identifier id, Identifier userId, EmailAddress emailAddress)
        {
            return new EmailAddressChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                EmailAddress = emailAddress
            };
        }

        public required string EmailAddress { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class ContactAddressChanged : IDomainEvent
    {
        public static ContactAddressChanged Create(Identifier id, Identifier userId, Address address)
        {
            return new ContactAddressChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                Line1 = address.Line1,
                Line2 = address.Line2,
                Line3 = address.Line3,
                City = address.City,
                State = address.State,
                CountryCode = address.CountryCode.Alpha3,
                Zip = address.Zip
            };
        }

        public string? City { get; set; }

        public required string CountryCode { get; set; }

        public string? Line1 { get; set; }

        public string? Line2 { get; set; }

        public string? Line3 { get; set; }

        public string? State { get; set; }

        public required string UserId { get; set; }

        public string? Zip { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class TimezoneChanged : IDomainEvent
    {
        public static TimezoneChanged Create(Identifier id, Identifier userId, Timezone timezone)
        {
            return new TimezoneChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                Timezone = timezone.Code.ToString()
            };
        }

        public required string Timezone { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class NameChanged : IDomainEvent
    {
        public static NameChanged Create(Identifier id, Identifier userId, PersonName name)
        {
            return new NameChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault!
            };
        }

        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class DisplayNameChanged : IDomainEvent
    {
        public static DisplayNameChanged Create(Identifier id, Identifier userId, PersonDisplayName name)
        {
            return new DisplayNameChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                DisplayName = name
            };
        }

        public required string DisplayName { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class PhoneNumberChanged : IDomainEvent
    {
        public static PhoneNumberChanged Create(Identifier id, Identifier userId, PhoneNumber number)
        {
            return new PhoneNumberChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                UserId = userId,
                Number = number
            };
        }

        public required string Number { get; set; }

        public required string UserId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }
}