using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderStatusSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsStatus()
    {
        var canceled = DateTime.UtcNow;

        var result = ProviderStatus.Create(BillingSubscriptionStatus.Unsubscribed, canceled, true);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Unsubscribed);
        result.Value.CanceledDateUtc.Should().Be(canceled);
        result.Value.CanBeCanceled.Should().Be(false);
        result.Value.CanBeUnsubscribed.Should().Be(true);
    }

    [Fact]
    public void WhenEmpty_ThenCannotBeCanceledOrUnsubscribed()
    {
        var result = ProviderStatus.Empty;

        result.Status.Should().Be(BillingSubscriptionStatus.Unsubscribed);
        result.CanBeCanceled.Should().BeFalse();
        result.CanBeUnsubscribed.Should().BeFalse();
    }

    [Fact]
    public void WhenActive_ThenCanBeCanceled()
    {
        var result = ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(BillingSubscriptionStatus.Activated);
        result.Value.CanBeCanceled.Should().BeTrue();
        result.Value.CanBeUnsubscribed.Should().BeTrue();
    }
}