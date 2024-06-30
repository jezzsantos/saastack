using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderInvoiceSpec
{
    [Fact]
    public void WhenCreateWithNegativeAmount_ThenReturnsError()
    {
        var result = ProviderInvoice.Create(-1, CurrencyCode.Create(CurrencyCodes.NewZealandDollar).Value,
            Optional<DateTime>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.BillingInvoice_InvalidAmount);
    }

    [Fact]
    public void WhenCreate_ThenCreatesInvoice()
    {
        var next = DateTime.UtcNow;

        var result = ProviderInvoice.Create(1, CurrencyCode.Create(CurrencyCodes.NewZealandDollar).Value, next);

        result.Value.Amount.Should().Be(1);
        result.Value.CurrencyCode.Currency.Should().Be(CurrencyCodes.UnitedStatesDollar);
        result.Value.NextUtc.Should().Be(next);
    }
}