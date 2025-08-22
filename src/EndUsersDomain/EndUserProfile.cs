using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using JetBrains.Annotations;

namespace EndUsersDomain;

public sealed class EndUserProfile : ValueObjectBase<EndUserProfile>
{
    public static Result<EndUserProfile, Error> Create(string firstName, string? lastName = null,
        string? timezone = null, string? locale = null, string? countryCode = null)
    {
        var name = PersonName.Create(firstName, lastName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var tz = Timezone.Create(Timezones.FindOrDefault(timezone));
        if (tz.IsFailure)
        {
            return tz.Error;
        }

        var loc = Locale.Create(Locales.FindOrDefault(locale));
        if (loc.IsFailure)
        {
            return loc.Error;
        }

        var address = Address.Create(CountryCodes.FindOrDefault(countryCode));
        if (address.IsFailure)
        {
            return address.Error;
        }

        return new EndUserProfile(name.Value, tz.Value, loc.Value, address.Value);
    }

    private EndUserProfile(PersonName name, Timezone timezone, Locale locale, Address address)
    {
        Name = name;
        Timezone = timezone;
        Locale = locale;
        Address = address;
    }

    public Address Address { get; }

    public Locale Locale { get; }

    public PersonName Name { get; }

    public Timezone Timezone { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<EndUserProfile> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new EndUserProfile(
                PersonName.Rehydrate()(parts[0], container),
                Timezone.Rehydrate()(parts[1], container),
                parts[3].HasValue
                    ? Locale.Rehydrate()(parts[3], container)
                    : Locale.Create(Locales.Default).Value, //for backwards compatibility
                Address.Rehydrate()(parts[2], container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Name, Timezone, Address, Locale];
    }
}