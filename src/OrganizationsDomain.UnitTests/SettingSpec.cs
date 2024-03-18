using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SettingSpec
{
    [Fact]
    public void WhenCreateWithEmptyStringValue_ThenReturnsSetting()
    {
        var result = Setting.Create(string.Empty, true);

        result.Should().BeSuccess();
        result.Value.Value.As<string>().Should().BeEmpty();
        result.Value.ValueType.Should().Be(SettingValueType.String);
        result.Value.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithStringValue_ThenReturnsSetting()
    {
        var result = Setting.Create("aname", true);

        result.Should().BeSuccess();
        result.Value.Value.Should().Be("aname");
        result.Value.ValueType.Should().Be(SettingValueType.String);
        result.Value.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithIntegerValue_ThenReturnsSetting()
    {
        var result = Setting.Create(99);

        result.Should().BeSuccess();
        result.Value.Value.Should().Be(99);
        result.Value.ValueType.Should().Be(SettingValueType.Number);
        result.Value.IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateWithFloatValue_ThenReturnsSetting()
    {
        var result = Setting.Create(99.99);

        result.Should().BeSuccess();
        result.Value.Value.Should().Be(99.99);
        result.Value.ValueType.Should().Be(SettingValueType.Number);
        result.Value.IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateWithBooleanValue_ThenReturnsSetting()
    {
        var result = Setting.Create(true);

        result.Should().BeSuccess();
        result.Value.Value.Should().Be(true);
        result.Value.ValueType.Should().Be(SettingValueType.Boolean);
        result.Value.IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void WhenFromAndEncryptedString_ThenReturns()
    {
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("adecryptedvalue");

        var result = Setting.From("astringvalue", SettingValueType.String, true, tenantSettingService.Object);

        result.Value.Value.Should().Be("adecryptedvalue");
        result.Value.Value.Should().BeOfType<string>();
        result.Value.ValueType.Should().Be(SettingValueType.String);
        tenantSettingService.Verify(x => x.Decrypt("astringvalue"));
    }

    [Fact]
    public void WhenFromAndUnencryptedString_ThenReturns()
    {
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("adecryptedvalue");

        var result = Setting.From("astringvalue", SettingValueType.String, false, tenantSettingService.Object);

        result.Value.Value.Should().Be("astringvalue");
        result.Value.Value.Should().BeOfType<string>();
        result.Value.ValueType.Should().Be(SettingValueType.String);
        tenantSettingService.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenFromAndInteger_ThenReturns()
    {
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("adecryptedvalue");

        var result = Setting.From("99", SettingValueType.Number, false, tenantSettingService.Object);

        result.Value.Value.Should().Be(99D);
        result.Value.Value.Should().BeOfType<double>();
        result.Value.ValueType.Should().Be(SettingValueType.Number);
        tenantSettingService.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenFromAndFloat_ThenReturns()
    {
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("adecryptedvalue");

        var result = Setting.From("99.99", SettingValueType.Number, false, tenantSettingService.Object);

        result.Value.Value.Should().Be(99.99D);
        result.Value.Value.Should().BeOfType<double>();
        result.Value.ValueType.Should().Be(SettingValueType.Number);
        tenantSettingService.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenFromAndBoolean_ThenReturns()
    {
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("adecryptedvalue");

        var result = Setting.From("True", SettingValueType.Boolean, false, tenantSettingService.Object);

        result.Value.Value.Should().Be(true);
        result.Value.Value.Should().BeOfType<bool>();
        result.Value.ValueType.Should().Be(SettingValueType.Boolean);
        tenantSettingService.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
    }
}