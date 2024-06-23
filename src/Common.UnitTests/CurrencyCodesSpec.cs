using Common.Extensions;
using FluentAssertions;
using ISO._4217;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class CurrencyCodesSpec
{
    [Fact]
    public void WhenExistsAndUnknown_ThenReturnsFalse()
    {
        var result = CurrencyCodes.Exists("notacurrencycode");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenExistsByCode_ThenReturnsTrue()
    {
        var result = CurrencyCodes.Exists(CurrencyCodes.Default.Code);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenExistsByNumeric_ThenReturnsTrue()
    {
        var result = CurrencyCodes.Exists(CurrencyCodes.Default.Numeric);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFindAndUnknown_ThenReturnsNull()
    {
        var result = CurrencyCodes.Find("notacurrencycode");

        result.Should().BeNull();
    }

    [Fact]
    public void WhenFindByCode_ThenReturnsTrue()
    {
        var result = CurrencyCodes.Find(CurrencyCodes.Default.Code);

        result.Should().Be(CurrencyCodes.Default);
    }

    [Fact]
    public void WhenFindByNumeric_ThenReturnsTrue()
    {
        var result = CurrencyCodes.Find(CurrencyCodes.Default.Numeric);

        result.Should().Be(CurrencyCodes.Default);
    }

    [Fact]
    public void WhenFindForEveryCurrency_ThenReturnsCode()
    {
        var currencies = CurrencyCodesResolver.Codes
            .Where(cur => cur.Code.HasValue())
            .ToList();
        foreach (var currency in currencies)
        {
            var result = CurrencyCodes.Find(currency.Code);

            result.Should().NotBeNull($"{currency.Name} should have been found by Code");
        }

        foreach (var currency in currencies)
        {
            var result = CurrencyCodes.Find(currency.Num);

            result.Should().NotBeNull($"{currency.Name} should have been found by NumericCode");
        }
    }

    [Fact]
    public void WhenCreateIso4217_ThenReturnsInstance()
    {
        var result = CurrencyCodeIso4217.Create("ashortname", "analpha2", "100", CurrencyDecimalKind.TwoDecimal);

        result.ShortName.Should().Be("ashortname");
        result.Code.Should().Be("analpha2");
        result.Kind.Should().Be(CurrencyDecimalKind.TwoDecimal);
        result.Numeric.Should().Be("100");
    }

    [Fact]
    public void WhenEqualsAndNotTheSameNumeric_ThenReturnsFalse()
    {
        var currency1 = CurrencyCodeIso4217.Create("ashortname", "analpha2", "100", CurrencyDecimalKind.Unknown);
        var currency2 = CurrencyCodeIso4217.Create("ashortname", "analpha2", "101", CurrencyDecimalKind.Unknown);

        var result = currency1 == currency2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndSameNumeric_ThenReturnsTrue()
    {
        var currency1 = CurrencyCodeIso4217.Create("ashortname1", "analpha21", "100", CurrencyDecimalKind.Unknown);
        var currency2 = CurrencyCodeIso4217.Create("ashortname2", "analpha22", "100", CurrencyDecimalKind.Unknown);

        var result = currency1 == currency2;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenToCurrencyWithAThousandAndOne_ThenReturnsUnitedStatesDollars()
    {
        var code = CurrencyCodes.UnitedStatesDollar.Code;
        var result = CurrencyCodes.ToCurrency(code, 1001);

        result.Should().Be(10.01M);
    }

    [Fact]
    public void WhenToCurrencyWithAThousandAndOne_ThenReturnsKuwaitiDinars()
    {
        var code = CurrencyCodes.KuwaitiDinar.Code;
        var result = CurrencyCodes.ToCurrency(code, 1001);

        result.Should().Be(1.001M);
    }

    [Fact]
    public void WhenToCurrencyWithAThousandAndOne_ThenReturnsChileanFomentos()
    {
        var code = CurrencyCodes.ChileanFomento.Code;
        var result = CurrencyCodes.ToCurrency(code, 1001);

        result.Should().Be(0.1001M);
    }
}