using Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderSubscriptionSpec
{
    [Fact]
    public void WhenCreateWithEmptySubscriptionId_ThenReturnsError()
    {
        var result = ProviderSubscription.Create(Identifier.Empty(), ProviderStatus.Empty, ProviderPlan.Empty,
            ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderSubscription_InvalidSubscriptionId);
    }

    [Fact]
    public void WhenCreateWithStatus_ThenReturnsSubscription()
    {
        var status = ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value;

        var result = ProviderSubscription.Create(status);

        result.Value.Status.Should().Be(status);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.Plan.Should().Be(ProviderPlan.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.SubscriptionReference.Should().BeNone();
    }

    [Fact]
    public void WhenCreateWithStatusAndPaymentMethod_ThenReturnsSubscription()
    {
        var status = ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value;
        var paymentMethod = ProviderPaymentMethod
            .Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None).Value;

        var result = ProviderSubscription.Create(status, paymentMethod);

        result.Value.Status.Should().Be(status);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.Plan.Should().Be(ProviderPlan.Empty);
        result.Value.PaymentMethod.Should().Be(paymentMethod);
        result.Value.SubscriptionReference.Should().BeNone();
    }
}