using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanSpec
{
    [Fact]
    public void WhenCreateWithEmptyPlanId_ThenReturnsError()
    {
        var trialEnd = DateTime.UtcNow;

        var result = ProviderPlan.Create(string.Empty, true, trialEnd, BillingSubscriptionTier.Enterprise);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlan_InvalidPlanId);
    }

    [Fact]
    public void WhenCreate_ThenReturnsPlan()
    {
        var trialEnd = DateTime.UtcNow;

        var result = ProviderPlan.Create("anid", true, trialEnd, BillingSubscriptionTier.Enterprise);

        result.Should().BeSuccess();
        result.Value.PlanId.Should().Be("anid");
        result.Value.IsTrial.Should().BeTrue();
        result.Value.TrialEndDateUtc.Should().Be(trialEnd);
        result.Value.Tier.Should().Be(BillingSubscriptionTier.Enterprise);
    }
}