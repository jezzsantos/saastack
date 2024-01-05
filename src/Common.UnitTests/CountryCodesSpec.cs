using FluentAssertions;
using ISO._3166;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class CountryCodesSpec
{
    [Fact]
    public void WhenExistsAndUnknown_ThenReturnsFalse()
    {
        var result = CountryCodes.Exists("notacountrycode");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenExistsByAlpha2_ThenReturnsTrue()
    {
        var result = CountryCodes.Exists(CountryCodes.Default.Alpha2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenExistsByAlpha3_ThenReturnsTrue()
    {
        var result = CountryCodes.Exists(CountryCodes.Default.Alpha3);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenExistsByNumeric_ThenReturnsTrue()
    {
        var result = CountryCodes.Exists(CountryCodes.Default.Numeric);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFindAndUnknown_ThenReturnsNull()
    {
        var result = CountryCodes.Find("notacountrycode");

        result.Should().BeNull();
    }

    [Fact]
    public void WhenFindByAlpha2_ThenReturnsTrue()
    {
        var result = CountryCodes.Find(CountryCodes.Default.Alpha2);

        result.Should().Be(CountryCodes.Default);
    }

    [Fact]
    public void WhenFindByAlpha3_ThenReturnsTrue()
    {
        var result = CountryCodes.Find(CountryCodes.Default.Alpha3);

        result.Should().Be(CountryCodes.Default);
    }

    [Fact]
    public void WhenFindByNumeric_ThenReturnsTrue()
    {
        var result = CountryCodes.Find(CountryCodes.Default.Numeric);

        result.Should().Be(CountryCodes.Default);
    }

    [Fact]
    public void WhenFindForEveryCountryCode_ThenReturnsCode()
    {
        var countryCodes = CountryCodesResolver.GetList();
        foreach (var countryCode in countryCodes)
        {
            var result = CountryCodes.Find(countryCode.Alpha2);

            result.Should().NotBeNull($"{countryCode.Name} should have been found by Alpha2");
        }

        foreach (var countryCode in countryCodes)
        {
            var result = CountryCodes.Find(countryCode.Alpha3);

            result.Should().NotBeNull($"{countryCode.Name} should have been found by Alpha3");
        }

        foreach (var countryCode in countryCodes)
        {
            var result = CountryCodes.Find(countryCode.NumericCode);

            result.Should().NotBeNull($"{countryCode.Name} should have been found by NumericCode");
        }
    }

    [Fact]
    public void WhenCreateIso3166_ThenReturnsInstance()
    {
        var result = CountryCodeIso3166.Create("ashortname", "analpha2", "analpha3", "100");

        result.ShortName.Should().Be("ashortname");
        result.Alpha2.Should().Be("analpha2");
        result.Alpha3.Should().Be("analpha3");
        result.Numeric.Should().Be("100");
    }

    [Fact]
    public void WhenEqualsAndNotTheSameNumeric_ThenReturnsFalse()
    {
        var countryCode1 = CountryCodeIso3166.Create("ashortname", "analpha2", "analpha3", "100");
        var countryCode2 = CountryCodeIso3166.Create("ashortname", "analpha2", "analpha3", "101");

        var result = countryCode1 == countryCode2;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndSameNumeric_ThenReturnsTrue()
    {
        var countryCode1 = CountryCodeIso3166.Create("ashortname1", "analpha21", "analpha31", "100");
        var countryCode2 = CountryCodeIso3166.Create("ashortname2", "analpha22", "analpha32", "100");

        var result = countryCode1 == countryCode2;

        result.Should().BeTrue();
    }
}