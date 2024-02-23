using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SettingsSpec
{
    [Fact]
    public void WhenCreateWithEmptyCollection_ThenReturnsSettings()
    {
        var result = Settings.Create(new Dictionary<string, Setting>());

        result.Should().BeSuccess();
        result.Value.Properties.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithCollection_ThenReturnsSettings()
    {
        var result = Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("avalue1", true).Value },
            { "aname2", Setting.Create("avalue2", false).Value }
        });

        result.Should().BeSuccess();
        result.Value.Properties.Count.Should().Be(2);
        result.Value.Properties["aname1"].Value.Should().Be("avalue1");
        result.Value.Properties["aname1"].IsEncrypted.Should().BeTrue();
        result.Value.Properties["aname2"].Value.Should().Be("avalue2");
        result.Value.Properties["aname2"].IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void WhenAddOrUpdateAndNotExist_ThenAdds()
    {
        var settings = Settings.Create(new Dictionary<string, Setting>()).Value;

        var result = settings.AddOrUpdate("aname", "avalue", true);

        result.Should().BeSuccess();
        result.Value.Properties.Count.Should().Be(1);
        result.Value.Properties["aname"].Value.Should().Be("avalue");
        result.Value.Properties["aname"].IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void WhenAddOrUpdateAndExists_ThenUpdates()
    {
        var settings = Settings.Create(new Dictionary<string, Setting>
        {
            { "aname", Setting.Create("anoldvalue", true).Value }
        }).Value;

        var result = settings.AddOrUpdate("aname", "anewvalue", false);

        result.Should().BeSuccess();
        result.Value.Properties.Count.Should().Be(1);
        result.Value.Properties["aname"].Value.Should().Be("anewvalue");
        result.Value.Properties["aname"].IsEncrypted.Should().BeFalse();
    }
}