using Common.Extensions;
using ISO._4217;

namespace Common;

/// <summary>
///     See: https://en.wikipedia.org/wiki/ISO_4217#Numeric_codes
/// </summary>
public static class CurrencyCodes
{
    public static readonly CurrencyCodeIso4217 ChileanFomento =
        CurrencyCodeIso4217.Create("Unidad de Fomento", "CLF", "990", CurrencyDecimalKind.FourDecimal);
    public static readonly CurrencyCodeIso4217 NewZealandDollar =
        CurrencyCodeIso4217.Create("New Zealand dollar", "NZD", "554", CurrencyDecimalKind.TwoDecimal);
    public static readonly CurrencyCodeIso4217 Default = NewZealandDollar;
    public static readonly CurrencyCodeIso4217 KuwaitiDinar =
        CurrencyCodeIso4217.Create("Kuwaiti dinar", "KWD", "414", CurrencyDecimalKind.ThreeDecimal);

#if TESTINGONLY
    public static readonly CurrencyCodeIso4217 Test =
        CurrencyCodeIso4217.Create("Test", "XXX", "001", CurrencyDecimalKind.TwoDecimal);
#endif
    public static readonly CurrencyCodeIso4217 UnitedStatesDollar =
        CurrencyCodeIso4217.Create("United States dollar", "USD", "840", CurrencyDecimalKind.TwoDecimal);

    /// <summary>
    ///     Whether the specified currency by its <see cref="currencyCodeOrNumber" /> exists
    /// </summary>
    public static bool Exists(string currencyCodeOrNumber)
    {
        return Find(currencyCodeOrNumber).Exists();
    }

    /// <summary>
    ///     Returns the specified currency by its <see cref="currencyCodeOrNumber" /> if it exists
    /// </summary>
    public static CurrencyCodeIso4217? Find(string? currencyCodeOrNumber)
    {
        if (currencyCodeOrNumber.NotExists())
        {
            return null;
        }

#if TESTINGONLY
        if (currencyCodeOrNumber == Test.Code
            || currencyCodeOrNumber == Test.Numeric)
        {
            return Test;
        }
#endif

        var code = CurrencyCodesResolver.GetCurrenciesByCode(currencyCodeOrNumber)
            .FirstOrDefault(cur => cur.Code.HasValue());
        if (code.Exists())
        {
            return CurrencyCodeIso4217.Create(code.Name, code.Code, code.Num, code.Exponent.ToKind());
        }

        var numeric = CurrencyCodesResolver.GetCurrenciesByNumber(currencyCodeOrNumber)
            .FirstOrDefault(cur => cur.Code.HasValue());
        if (numeric.Exists())
        {
            return CurrencyCodeIso4217.Create(numeric.Name, numeric.Code, numeric.Num, numeric.Exponent.ToKind());
        }

        return null;
    }

    /// <summary>
    ///     Returns the specified currency by its <see cref="currencyCodeOrNumber" /> if it exists,
    ///     or returns <see cref="Default" />
    /// </summary>
    public static CurrencyCodeIso4217 FindOrDefault(string? currencyCodeOrNumber)
    {
        var exists = Find(currencyCodeOrNumber);
        return exists.Exists()
            ? exists
            : Default;
    }

    /// <summary>
    ///     Converts the amount in cents to a currency
    /// </summary>
    public static decimal ToCurrency(string code, int amountInCents)
    {
        var currency = Find(code);
        if (currency.NotExists())
        {
            return amountInCents;
        }

        return currency.Kind switch
        {
            CurrencyDecimalKind.ZeroDecimal => amountInCents,
            CurrencyDecimalKind.TwoDecimal => (decimal)amountInCents / 100,
            CurrencyDecimalKind.ThreeDecimal => (decimal)amountInCents / 1000,
            CurrencyDecimalKind.FourDecimal => (decimal)amountInCents / 10000,
            _ => amountInCents
        };
    }
}

/// <summary>
///     See: https://en.wikipedia.org/wiki/ISO_4217 for details
/// </summary>
public sealed class CurrencyCodeIso4217 : IEquatable<CurrencyCodeIso4217>
{
    private CurrencyCodeIso4217(string numeric, string shortName, string code)
    {
        numeric.ThrowIfInvalidParameter(num =>
        {
            if (!int.TryParse(num, out var integer))
            {
                return false;
            }

            return integer is >= 1 and < 1000;
        }, nameof(numeric), Resources.CurrencyIso4217_InvalidNumeric.Format(numeric));
        Numeric = numeric;
        ShortName = shortName;
        Code = code;
    }

    public string Code { get; }

    public CurrencyDecimalKind Kind { get; private init; }

    public string Numeric { get; }

    public string ShortName { get; }

    public bool Equals(CurrencyCodeIso4217? other)
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

    internal static CurrencyCodeIso4217 Create(string shortName, string code, string numeric, CurrencyDecimalKind kind)
    {
        shortName.ThrowIfNotValuedParameter(nameof(shortName));
        code.ThrowIfNotValuedParameter(nameof(code));

        var instance = new CurrencyCodeIso4217(numeric, shortName, code)
        {
            Kind = kind
        };

        return instance;
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

        return Equals((CurrencyCodeIso4217)obj);
    }

    public override int GetHashCode()
    {
        ArgumentException.ThrowIfNullOrEmpty(Numeric);

        return Numeric.GetHashCode();
    }

    public static bool operator ==(CurrencyCodeIso4217 left, CurrencyCodeIso4217 right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CurrencyCodeIso4217 left, CurrencyCodeIso4217 right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
///     The decimal kind of currency
/// </summary>
public enum CurrencyDecimalKind
{
    ZeroDecimal = 0,
    TwoDecimal = 2,
    ThreeDecimal = 3,
    FourDecimal = 4,
    Unknown = -1
}

internal static class ConversionExtensions
{
    public static CurrencyDecimalKind ToKind(this string exponent)
    {
        if (exponent.HasNoValue())
        {
            return CurrencyDecimalKind.Unknown;
        }

        var numeral = exponent.ToIntOrDefault(-1);
        return numeral switch
        {
            -1 => CurrencyDecimalKind.Unknown,
            0 => CurrencyDecimalKind.ZeroDecimal,
            2 => CurrencyDecimalKind.TwoDecimal,
            3 => CurrencyDecimalKind.ThreeDecimal,
            4 => CurrencyDecimalKind.FourDecimal,
            _ => CurrencyDecimalKind.Unknown
        };
    }
}