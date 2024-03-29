using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Domain.Shared;

public sealed class Address : ValueObjectBase<Address>
{
    public static readonly Address Default = Create(CountryCodes.Default).Value;

    public static Result<Address, Error> Create(string? line1, string? line2, string? line3, string? city,
        string? state,
        CountryCodeIso3166 countryCode, string? zip)
    {
        return new Address(line1, line2, line3, city, state, countryCode, zip);
    }

    public static Result<Address, Error> Create(CountryCodeIso3166 countryCode)
    {
        return new Address(countryCode);
    }

    private Address(CountryCodeIso3166 countryCode) : this(string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, countryCode, string.Empty)
    {
    }

    private Address(string? line1, string? line2, string? line3, string? city, string? state,
        CountryCodeIso3166 countryCode, string? zip)
    {
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
        City = city;
        State = state;
        CountryCode = countryCode;
        Zip = zip;
    }

    public string? City { get; }

    public CountryCodeIso3166 CountryCode { get; }

    public string? Line1 { get; }

    public string? Line2 { get; }

    public string? Line3 { get; }

    public string? State { get; }

    public string? Zip { get; }

    public static ValueObjectFactory<Address> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Address(parts[0], parts[1], parts[2], parts[3], parts[4],
                CountryCodes.FindOrDefault(parts[5]),
                parts[6]);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Line1, Line2, Line3, City, State, CountryCode.Alpha3, Zip };
    }
}