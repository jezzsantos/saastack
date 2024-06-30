using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class CurrencyCodeSpec
{
    [Fact]
    public void WhenCreateWithUnknownCurrency_ThenReturnsDefault()
    {
        var result = CurrencyCode.Create("anunknowncurrency");

        result.Should().BeSuccess();
        result.Value.Currency.Should().Be(CurrencyCodes.Default);
    }

    [Fact]
    public void WhenCreateWithKnownCurrency_ThenReturnsCurrency()
    {
        var result = CurrencyCode.Create(CurrencyCodes.NewZealandDollar.Code);

        result.Should().BeSuccess();
        result.Value.Currency.Should().Be(CurrencyCodes.NewZealandDollar);
    }

    [Fact]
    public void WhenCreateWithKnownCurrencyCode_ThenReturnsCurrency()
    {
        var result = CurrencyCode.Create(CurrencyCodes.NewZealandDollar);

        result.Should().BeSuccess();
        result.Value.Currency.Should().Be(CurrencyCodes.NewZealandDollar);
    }
}