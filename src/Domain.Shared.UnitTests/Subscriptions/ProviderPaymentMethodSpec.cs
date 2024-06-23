using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPaymentMethodSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsPaymentMethod()
    {
        var expires = DateOnly.FromDateTime(DateTime.UtcNow);
        var result =
            ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid, expires);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(BillingPaymentMethodType.Card);
        result.Value.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.ExpiresOn.Should().Be(expires);
    }
}