using Azure.Identity;
using Common.Configuration;
using FluentAssertions;
using Infrastructure.External.Persistence.Azure.ApplicationServices;
using Moq;
using Xunit;

namespace Infrastructure.External.Persistence.Azure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class AzureServiceBusStoreOptionsSpec
{
    private readonly Mock<IConfigurationSettings> _settings;

    public AzureServiceBusStoreOptionsSpec()
    {
        _settings = new Mock<IConfigurationSettings>();
    }

    [Fact]
    public void WhenCredentials_ThenUsesConnectionString()
    {
        _settings.Setup(s => s.GetString(AzureServiceBusStoreOptions.ConnectionStringSettingName, It.IsAny<string>()))
            .Returns("aconnectionstring");

        var result = AzureServiceBusStoreOptions.Credentials(_settings.Object);

        result.Connection.Type.Should().Be(AzureServiceBusStoreOptions.ConnectionOptions.ConnectionType.Credentials);
        result.Connection.ConnectionString.Should().Be("aconnectionstring");
        result.Connection.Credential.Should().BeNull();
        result.Connection.NamespaceName.Should().BeNull();
        _settings.Verify(s => s.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _settings.Verify(s =>
            s.GetString(AzureServiceBusStoreOptions.ConnectionStringSettingName, null));
    }

    [Fact]
    public void WhenUserManagedIdentity_ThenUsesCredentials()
    {
        _settings.Setup(s => s.GetString(AzureServiceBusStoreOptions.NamespaceNameSettingName, It.IsAny<string>()))
            .Returns("anamespace");
        _settings.Setup(s =>
                s.GetString(AzureServiceBusStoreOptions.ManagedIdentityClientIdSettingName, It.IsAny<string>()))
            .Returns("aclientid");

        var result = AzureServiceBusStoreOptions.UserManagedIdentity(_settings.Object);

        result.Connection.Type.Should()
            .Be(AzureServiceBusStoreOptions.ConnectionOptions.ConnectionType.ManagedIdentity);
        result.Connection.ConnectionString.Should().BeNull();
        result.Connection.Credential.Should().BeOfType<DefaultAzureCredential>();
        result.Connection.NamespaceName.Should().Be("anamespace");
        _settings.Verify(s => s.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _settings.Verify(s =>
            s.GetString(AzureServiceBusStoreOptions.NamespaceNameSettingName, null));
        _settings.Verify(s =>
            s.GetString(AzureServiceBusStoreOptions.ManagedIdentityClientIdSettingName, null));
    }
}