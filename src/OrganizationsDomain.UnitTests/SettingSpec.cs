using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SettingSpec
{
    [Fact]
    public void WhenCreateWithEmptyValue_ThenReturnsSetting()
    {
        var result = Setting.Create(string.Empty, true);

        result.Should().BeSuccess();
        result.Value.Value.Should().BeEmpty();
        result.Value.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithNonEmptyValue_ThenReturnsSetting()
    {
        var result = Setting.Create("aname", true);

        result.Should().BeSuccess();
        result.Value.Value.Should().Be("aname");
        result.Value.IsEncrypted.Should().BeTrue();
    }
}