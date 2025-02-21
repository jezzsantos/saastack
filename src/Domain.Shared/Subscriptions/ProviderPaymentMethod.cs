using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPaymentMethod : ValueObjectBase<ProviderPaymentMethod>
{
    public static readonly ProviderPaymentMethod Empty =
        Create(BillingPaymentMethodType.None, BillingPaymentMethodStatus.Invalid, Optional<DateOnly>.None).Value;

    public static Result<ProviderPaymentMethod, Error> Create(BillingPaymentMethodType type,
        BillingPaymentMethodStatus status, Optional<DateOnly> expiresOn)
    {
        return new ProviderPaymentMethod(type, status, expiresOn);
    }

    private ProviderPaymentMethod(BillingPaymentMethodType type, BillingPaymentMethodStatus status,
        Optional<DateOnly> expiresOn)
    {
        Type = type;
        Status = status;
        ExpiresOn = expiresOn;
    }

    public Optional<DateOnly> ExpiresOn { get; }

    public BillingPaymentMethodStatus Status { get; }

    public BillingPaymentMethodType Type { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPaymentMethod> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPaymentMethod(
                parts[0].ToEnumOrDefault(BillingPaymentMethodType.None),
                parts[1].ToEnumOrDefault(BillingPaymentMethodStatus.Invalid),
                parts[2].FromValueOrNone(value => value.FromIso8601DateOnly()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[]
        {
            Type, Status, ExpiresOn.ToValueOrNull(val => val.ToIso8601())
        };
    }
}

public enum BillingPaymentMethodType
{
    None = 0,
    Card = 1, // debit or credit
    Other = 2
}

public enum BillingPaymentMethodStatus
{
    Invalid = 0,
    Valid = 1
}