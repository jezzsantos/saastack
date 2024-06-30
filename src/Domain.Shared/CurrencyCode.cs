using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Domain.Shared;

public sealed class CurrencyCode : SingleValueObjectBase<CurrencyCode, string>
{
    public static Result<CurrencyCode, Error> Create(CurrencyCodeIso4217 code)
    {
        return new CurrencyCode(code);
    }

    public static Result<CurrencyCode, Error> Create(string code)
    {
        return new CurrencyCode(CurrencyCodes.FindOrDefault(code));
    }

    private CurrencyCode(CurrencyCodeIso4217 code) : this(code.Code)
    {
    }

    private CurrencyCode(string code) : base(code)
    {
    }

    public CurrencyCodeIso4217 Currency => CurrencyCodes.FindOrDefault(Value);

    public static CurrencyCode Default => new(CurrencyCodes.Default.ToString()!);

    public static ValueObjectFactory<CurrencyCode> Rehydrate()
    {
        return (property, _) => new CurrencyCode(property);
    }
}