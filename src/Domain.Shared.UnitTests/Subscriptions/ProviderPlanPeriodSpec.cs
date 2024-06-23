using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanPeriodSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsPeriod()
    {
        var result = ProviderPlanPeriod.Create(1, BillingFrequencyUnit.Day);

        result.Should().BeSuccess();
        result.Value.Frequency.Should().Be(1);
        result.Value.Unit.Should().Be(BillingFrequencyUnit.Day);
    }
}