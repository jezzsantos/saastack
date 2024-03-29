using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationRootSpec
{
    private readonly OrganizationRoot _org;

    public OrganizationRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);

        _org = OrganizationRoot.Create(recorder.Object, identifierFactory.Object, tenantSettingService.Object,
            Ownership.Personal, "acreatorid".ToId(), DisplayName.Create("aname").Value).Value;
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        _org.Name.Name.Should().Be("aname");
        _org.CreatedById.Should().Be("acreatorid".ToId());
        _org.Ownership.Should().Be(Ownership.Personal);
        _org.Settings.Should().Be(Settings.Empty);
    }

    [Fact]
    public void WhenCreateSettings_ThenAddsSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("avalue1", true).Value },
            { "aname2", Setting.Create("avalue2", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(2);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("avalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("avalue2", true).Value);
        _org.Events.Last().Should().BeOfType<Events.SettingCreated>();
    }

    [Fact]
    public void WhenUpdateSettings_ThenAddsAndUpdatesSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anoldvalue1", false).Value },
            { "aname2", Setting.Create("anoldvalue2", false).Value }
        }).Value);
        _org.UpdateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anewvalue1", true).Value },
            { "aname3", Setting.Create("anewvalue3", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(3);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("anewvalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("anoldvalue2", false).Value);
        _org.Settings.Properties["aname3"].Should().Be(Setting.Create("anewvalue3", true).Value);
        _org.Events[3].Should().BeOfType<Events.SettingUpdated>();
        _org.Events.Last().Should().BeOfType<Events.SettingCreated>();
    }
}