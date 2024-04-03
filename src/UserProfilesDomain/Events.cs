using Domain.Common.ValueObjects;
using Domain.Events.Shared.UserProfiles;
using Domain.Shared;

namespace UserProfilesDomain;

public static class Events
{
    public static ContactAddressChanged ContactAddressChanged(Identifier id, Identifier userId, Address address)
    {
        return new ContactAddressChanged(id)
        {
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

    public static Created Created(Identifier id, ProfileType type, Identifier userId, PersonName name)
    {
        return new Created(id)
        {
            UserId = userId,
            FirstName = name.FirstName,
            LastName = name.LastName.ValueOrDefault!,
            DisplayName = name.FirstName,
            Type = type.ToString()
        };
    }

    public static DisplayNameChanged DisplayNameChanged(Identifier id, Identifier userId, PersonDisplayName name)
    {
        return new DisplayNameChanged(id)
        {
            UserId = userId,
            DisplayName = name
        };
    }

    public static EmailAddressChanged EmailAddressChanged(Identifier id, Identifier userId, EmailAddress emailAddress)
    {
        return new EmailAddressChanged(id)
        {
            UserId = userId,
            EmailAddress = emailAddress
        };
    }

    public static NameChanged NameChanged(Identifier id, Identifier userId, PersonName name)
    {
        return new NameChanged(id)
        {
            UserId = userId,
            FirstName = name.FirstName,
            LastName = name.LastName.ValueOrDefault!
        };
    }

    public static PhoneNumberChanged PhoneNumberChanged(Identifier id, Identifier userId, PhoneNumber number)
    {
        return new PhoneNumberChanged(id)
        {
            UserId = userId,
            Number = number
        };
    }

    public static TimezoneChanged TimezoneChanged(Identifier id, Identifier userId, Timezone timezone)
    {
        return new TimezoneChanged(id)
        {
            UserId = userId,
            Timezone = timezone.Code.ToString()
        };
    }
}