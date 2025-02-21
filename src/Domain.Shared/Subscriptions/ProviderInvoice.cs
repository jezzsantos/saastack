using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderInvoice : ValueObjectBase<ProviderPlanPeriod>
{
    public static readonly ProviderInvoice Default = new(0M, CurrencyCode.Create(CurrencyCodes.Default).Value,
        Optional<DateTime>.None);

    public static Result<ProviderInvoice, Error> Create(decimal amount, CurrencyCode currency,
        Optional<DateTime> nextUtc)
    {
        if (amount.IsInvalidParameter(num => num > 0, nameof(amount), Resources.BillingInvoice_InvalidAmount,
                out var error1))
        {
            return error1;
        }

        return new ProviderInvoice(amount, currency, nextUtc);
    }

    public static Result<ProviderInvoice, Error> Create(decimal amount, string currencyCode, Optional<DateTime> nextUtc)
    {
        if (amount.IsInvalidParameter(num => num >= 0, nameof(amount), Resources.BillingInvoice_InvalidAmount,
                out var error1))
        {
            return error1;
        }

        var currency = CurrencyCode.Create(currencyCode);
        if (currency.IsFailure)
        {
            return currency.Error;
        }

        return new ProviderInvoice(amount, currency.Value, nextUtc);
    }

    private ProviderInvoice(decimal amount, CurrencyCode currencyCode, Optional<DateTime> nextUtc)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
        NextUtc = nextUtc;
    }

    public decimal Amount { get; }

    public CurrencyCode CurrencyCode { get; }

    public Optional<DateTime> NextUtc { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderInvoice> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderInvoice(parts[0].ToDecimalOrDefault(0),
                CurrencyCode.Rehydrate()(parts[1]!, container),
                parts[2].FromValueOrNone(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { Amount, CurrencyCode.ToString(), NextUtc.ValueOrNull };
    }
}