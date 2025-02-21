using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlanPeriod : ValueObjectBase<ProviderPlanPeriod>
{
    public static readonly ProviderPlanPeriod Empty = Create(0, BillingFrequencyUnit.Eternity).Value;

    public static Result<ProviderPlanPeriod, Error> Create(int frequency, BillingFrequencyUnit unit)
    {
        if (frequency.IsInvalidParameter(val => val >= 0, nameof(frequency), Resources.BillingPeriod_InvalidFrequency,
                out var error))
        {
            return error;
        }

        return new ProviderPlanPeriod(frequency, unit);
    }

    private ProviderPlanPeriod(int frequency, BillingFrequencyUnit unit)
    {
        Frequency = frequency;
        Unit = unit;
    }

    public int Frequency { get; }

    public BillingFrequencyUnit Unit { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanPeriod> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlanPeriod(parts[0].ToIntOrDefault(0),
                parts[1].ToEnumOrDefault(BillingFrequencyUnit.Eternity));
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { Frequency, Unit };
    }
}

public enum BillingFrequencyUnit
{
    Eternity = 0,
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4
}