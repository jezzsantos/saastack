using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class LocaleSpec
{
    [Fact]
    public void WhenCreateAndInvalid_ThenReturnsError()
    {
        var result = Locale.Create("notalocale");

        result.Should().BeError(ErrorCode.Validation, Resources.Locale_InvalidLocale);
    }

    [Fact]
    public void WhenCreateWithLanguageOnly_ThenCreates()
    {
        var result = Locale.Create("en");

        result.Should().BeSuccess();
        result.Value.Code.Should().Be(Bcp47Locale.Create("en", null, null));
    }

    [Fact]
    public void WhenCreateWithDefault_ThenCreates()
    {
        var result = Locale.Create(Locales.Default);

        result.Should().BeSuccess();
        result.Value.Code.Should().Be(Locales.Default);
    }
}