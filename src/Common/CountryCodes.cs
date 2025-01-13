using Common.Extensions;
using ISO._3166;

namespace Common;

public static class CountryCodes
{
    public static readonly CountryCodeIso3166 Australia = CountryCodeIso3166.Create("Australia", "AU", "AUS", "036");
    public static readonly CountryCodeIso3166 UnitedStates =
        CountryCodeIso3166.Create("United States of America", "US", "USA", "840");
    public static readonly CountryCodeIso3166 Default = UnitedStates; //EXTEND: set your default country code
    public static readonly CountryCodeIso3166 NewZealand = CountryCodeIso3166.Create("New Zealand", "NZ", "NZL", "554");

#if TESTINGONLY
    internal static readonly CountryCodeIso3166 Test = CountryCodeIso3166.Create("Test", "XX", "XXX", "001");
#endif

    /// <summary>
    ///     Returns the specified timezone by its <see cref="countryCode" /> if it exists,
    ///     which tries to match the 3 letter <see cref="CountryCodeIso3166.Alpha3" /> first,
    ///     then tries to match the 2 letter <see cref="CountryCodeIso3166.Alpha2" />,
    ///     then tries to match the number <see cref="CountryCodeIso3166.Numeric" /> last.
    /// </summary>
    public static bool Exists(string? countryCode)
    {
        if (countryCode.NotExists())
        {
            return false;
        }

        return Find(countryCode).Exists();
    }

    /// <summary>
    ///     Returns the specified timezone by its <see cref="countryCode" /> if it exists,
    ///     which tries to match the 3 letter <see cref="CountryCodeIso3166.Alpha3" /> first,
    ///     then tries to match the 2 letter <see cref="CountryCodeIso3166.Alpha2" />,
    ///     then tries to match the number <see cref="CountryCodeIso3166.Numeric" /> last.
    /// </summary>
    public static CountryCodeIso3166? Find(string? countryCode)
    {
        if (countryCode.NotExists())
        {
            return null;
        }

#if TESTINGONLY
        if (countryCode == Test.Alpha3
            || countryCode == Test.Alpha2
            || countryCode == Test.Numeric)
        {
            return Test;
        }
#endif
        var alpha3 = CountryCodesResolver.GetByAlpha3Code(countryCode);
        if (alpha3.Exists())
        {
            return CountryCodeIso3166.Create(alpha3.Name, alpha3.Alpha2, alpha3.Alpha3, alpha3.NumericCode);
        }

        var alpha2 = CountryCodesResolver.GetByAlpha2Code(countryCode);
        if (alpha2.Exists())
        {
            return CountryCodeIso3166.Create(alpha2.Name, alpha2.Alpha2, alpha2.Alpha3, alpha2.NumericCode);
        }

        var numeric = CountryCodesResolver.GetList().FirstOrDefault(cc => cc.NumericCode == countryCode);
        if (numeric.Exists())
        {
            return CountryCodeIso3166.Create(numeric.Name, numeric.Alpha2, numeric.Alpha3, numeric.NumericCode);
        }

        return null;
    }

    /// <summary>
    ///     Returns the specified timezone by its <see cref="countryCodeAlpha3" /> if it exists,
    ///     or returns <see cref="Default" />
    /// </summary>
    public static CountryCodeIso3166 FindOrDefault(string? countryCodeAlpha3)
    {
        var exists = Find(countryCodeAlpha3);
        return exists.Exists()
            ? exists
            : Default;
    }
}

/// <summary>
///     See: https://en.wikipedia.org/wiki/ISO_3166-1 for details
/// </summary>
public sealed class CountryCodeIso3166 : IEquatable<CountryCodeIso3166>
{
    internal static CountryCodeIso3166 Create(string shortName, string alpha2,
        string alpha3, string numeric)
    {
        shortName.ThrowIfNotValuedParameter(nameof(shortName));
        alpha2.ThrowIfNotValuedParameter(nameof(alpha2));
        alpha3.ThrowIfNotValuedParameter(nameof(alpha3));

        var instance = new CountryCodeIso3166(numeric, shortName, alpha2, alpha3);

        return instance;
    }

    private CountryCodeIso3166(string numeric, string shortName, string alpha2, string alpha3)
    {
        numeric.ThrowIfInvalidParameter(num =>
        {
            if (!int.TryParse(num, out var integer))
            {
                return false;
            }

            return integer is >= 1 and < 1000;
        }, nameof(numeric), Resources.CountryCodeIso3166_InvalidNumeric.Format(numeric));
        Numeric = numeric;
        ShortName = shortName;
        Alpha2 = alpha2;
        Alpha3 = alpha3;
    }

    public string Alpha2 { get; }

    public string Alpha3 { get; }

    public string Numeric { get; }

    public string ShortName { get; private set; }

    public bool Equals(CountryCodeIso3166? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Numeric == other.Numeric;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((CountryCodeIso3166)obj);
    }

    public override int GetHashCode()
    {
        return Numeric.GetHashCode();
    }

    public static bool operator ==(CountryCodeIso3166 left, CountryCodeIso3166 right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CountryCodeIso3166 left, CountryCodeIso3166 right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns the <see cref="Alpha3" /> of the country
    /// </summary>
    public override string ToString()
    {
        return Alpha3;
    }
}