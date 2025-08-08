using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class LocaleSpec
{
    [Fact]
    public void WhenExistsAndUnknown_ThenReturnsFalse()
    {
        var result = Locales.Exists("notalocale");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenExistsAsJustLanguage_ThenReturnsTrue()
    {
        var result = Locales.Exists("en");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenExistsAsLanguageAndRegion_ThenReturnsTrue()
    {
        var result = Locales.Exists("en-US");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFindForUnknown_ThenReturnsNull()
    {
        var result = Locales.Find("notatimezone");

        result.Should().BeNull();
    }

    [Fact]
    public void WhenFindForDefault_ThenReturnsLocale()
    {
        var result = Locales.Find(Locales.Default.ToString());

        result!.LanguageCode.Should().Be("en");
        result.ScriptCode.Should().BeNull();
        result.RegionCode.Should().Be("US");
        result.ToString().Should().Be("en-US");
    }

    [Fact]
    public void WhenCreateAsLanguage_ThenThrows()
    {
        var result = Bcp47Locale.Create("en", null, null);

        result.LanguageCode.Should().Be("en");
        result.ScriptCode.Should().BeNull();
        result.RegionCode.Should().BeNull();
        result.ToString().Should().Be("en");
    }

    [Fact]
    public void WhenCreateAsLanguageAndScript_ThenThrows()
    {
        var result = Bcp47Locale.Create("en", "Latn", null);

        result.LanguageCode.Should().Be("en");
        result.ScriptCode.Should().Be("Latn");
        result.RegionCode.Should().BeNull();
        result.ToString().Should().Be("en-Latn");
    }

    [Fact]
    public void WhenCreateAsLanguageScriptAndRegion_ThenThrows()
    {
        var result = Bcp47Locale.Create("en", "Latn", "US");

        result.LanguageCode.Should().Be("en");
        result.ScriptCode.Should().Be("Latn");
        result.RegionCode.Should().Be("US");
        result.ToString().Should().Be("en-Latn-US");
    }
}