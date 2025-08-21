using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderSubscription : ValueObjectBase<ProviderSubscription>
{
    public static readonly ProviderSubscription Empty = Create(ProviderStatus.Empty).Value;

    public static Result<ProviderSubscription, Error> Create(ProviderStatus status)

    {
        return new ProviderSubscription(Optional<Identifier>.None, status, ProviderPlan.Empty, ProviderPlanPeriod.Empty,
            ProviderInvoice.Default, ProviderPaymentMethod.Empty);
    }

    public static Result<ProviderSubscription, Error> Create(ProviderStatus status, ProviderPaymentMethod paymentMethod)
    {
        return new ProviderSubscription(Optional<Identifier>.None, status, ProviderPlan.Empty, ProviderPlanPeriod.Empty,
            ProviderInvoice.Default, paymentMethod);
    }

    public static Result<ProviderSubscription, Error> Create(Identifier subscriptionId, ProviderStatus status,
        ProviderPlan plan, ProviderPlanPeriod period, ProviderInvoice invoice, ProviderPaymentMethod paymentMethod)
    {
        if (subscriptionId.IsInvalidParameter(id => !id.IsEmpty(), nameof(subscriptionId),
                Resources.ProviderSubscription_InvalidSubscriptionId, out var error))
        {
            return error;
        }

        return new ProviderSubscription(subscriptionId, status, plan, period, invoice, paymentMethod);
    }

    private ProviderSubscription(Optional<Identifier> subscriptionReference, ProviderStatus status, ProviderPlan plan,
        ProviderPlanPeriod period, ProviderInvoice invoice, ProviderPaymentMethod paymentMethod)
    {
        SubscriptionReference = subscriptionReference;
        Status = status;
        Plan = plan;
        Period = period;
        Invoice = invoice;
        PaymentMethod = paymentMethod;
    }

    public ProviderInvoice Invoice { get; }

    public ProviderPaymentMethod PaymentMethod { get; }

    public ProviderPlanPeriod Period { get; }

    public ProviderPlan Plan { get; }

    public ProviderStatus Status { get; }

    public Optional<Identifier> SubscriptionReference { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderSubscription> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderSubscription(parts[0].ToOptional(val => val.ToId()),
                ProviderStatus.Rehydrate()(parts[1]!, container),
                ProviderPlan.Rehydrate()(parts[2]!, container),
                ProviderPlanPeriod.Rehydrate()(parts[3]!, container),
                ProviderInvoice.Rehydrate()(parts[4]!, container),
                ProviderPaymentMethod.Rehydrate()(parts[5]!, container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [SubscriptionReference, Status, Plan, Period, Invoice, PaymentMethod];
    }
}