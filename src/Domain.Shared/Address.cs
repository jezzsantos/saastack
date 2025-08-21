using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

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

    private Address(Optional<string> line1, Optional<string> line2, Optional<string> line3, Optional<string> city,
        Optional<string> state, CountryCodeIso3166 countryCode, Optional<string> zip)
    {
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
        City = city;
        State = state;
        CountryCode = countryCode;
        Zip = zip;
    }

    public Optional<string> City { get; }

    public CountryCodeIso3166 CountryCode { get; }

    public Optional<string> Line1 { get; }

    public Optional<string> Line2 { get; }

    public Optional<string> Line3 { get; }

    public Optional<string> State { get; }

    public Optional<string> Zip { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<Address> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Address(parts[0], parts[1], parts[2], parts[3], parts[4],
                CountryCodes.FindOrDefault(parts[5]), parts[6]);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Line1, Line2, Line3, City, State, CountryCode.Alpha3, Zip];
    }
}