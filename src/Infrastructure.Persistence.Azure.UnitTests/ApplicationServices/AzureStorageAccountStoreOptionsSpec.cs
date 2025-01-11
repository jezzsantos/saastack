using Azure.Identity;
using Common.Configuration;
using FluentAssertions;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Moq;
using Xunit;

namespace Infrastructure.Persistence.Azure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class AzureStorageAccountStoreOptionsSpec
{
    private readonly Mock<IConfigurationSettings> _settings;

    public AzureStorageAccountStoreOptionsSpec()
    {
        _settings = new Mock<IConfigurationSettings>();
    }

    [Fact]
    public void WhenCredentials_ThenUsesConnectionString()
    {
        _settings.Setup(s => s.GetString(AzureStorageAccountStoreOptions.AccountNameSettingName, It.IsAny<string>()))
            .Returns("anaccountname");
        _settings.Setup(s => s.GetString(AzureStorageAccountStoreOptions.AccountKeySettingName, It.IsAny<string>()))
            .Returns("anaccountkey");

        var result = AzureStorageAccountStoreOptions.Credentials(_settings.Object);

        result.Connection.Type.Should()
            .Be(AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.Credentials);
        result.Connection.ConnectionString.Should().Be("DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=anaccountname;AccountKey=anaccountkey");
        result.Connection.Credential.Should().BeNull();
        result.Connection.AccountName.Should().BeNull();
        _settings.Verify(s => s.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _settings.Verify(s =>
            s.GetString(AzureStorageAccountStoreOptions.AccountNameSettingName, null));
        _settings.Verify(s =>
            s.GetString(AzureStorageAccountStoreOptions.AccountKeySettingName, null));
    }

    [Fact]
    public void WhenCustomConnectionStringThenUsesConnectionString()
    {
        var result = AzureStorageAccountStoreOptions.CustomConnectionString("aconnectionstring");

        result.Connection.Type.Should()
            .Be(AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.Credentials);
        result.Connection.ConnectionString.Should().Be("aconnectionstring");
        result.Connection.Credential.Should().BeNull();
        result.Connection.AccountName.Should().BeNull();
        _settings.Verify(s => s.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenUserManagedIdentity_ThenUsesCredentials()
    {
        _settings.Setup(s => s.GetString(AzureStorageAccountStoreOptions.AccountNameSettingName, It.IsAny<string>()))
            .Returns("anaccountname");
        _settings.Setup(s =>
                s.GetString(AzureStorageAccountStoreOptions.ManagedIdentityClientIdSettingName, It.IsAny<string>()))
            .Returns("aclientid");

        var result = AzureStorageAccountStoreOptions.UserManagedIdentity(_settings.Object);

        result.Connection.Type.Should()
            .Be(AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.ManagedIdentity);
        result.Connection.ConnectionString.Should().BeNull();
        result.Connection.Credential.Should().BeOfType<DefaultAzureCredential>();
        result.Connection.AccountName.Should().Be("anaccountname");
        _settings.Verify(s => s.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _settings.Verify(s =>
            s.GetString(AzureStorageAccountStoreOptions.AccountNameSettingName, null));
        _settings.Verify(s =>
            s.GetString(AzureStorageAccountStoreOptions.ManagedIdentityClientIdSettingName, null));
    }
}