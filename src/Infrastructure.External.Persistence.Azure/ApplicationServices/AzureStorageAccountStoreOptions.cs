using Azure.Identity;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.External.Persistence.Azure.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="AzureStorageAccountQueueStore" />
///     and <see cref="AzureStorageAccountBlobStore" />
/// </summary>
public class AzureStorageAccountStoreOptions
{
    internal const string AccountKeySettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountKey";
    internal const string AccountNameSettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountName";
    internal const string DefaultConnectionString = "UseDevelopmentStorage=true";
    internal const string ManagedIdentityClientIdSettingName =
        "ApplicationServices:Persistence:AzureStorageAccount:ManagedIdentityClientId";

    private AzureStorageAccountStoreOptions(string connectionString)
    {
        Connection = new ConnectionOptions(connectionString);
    }

    private AzureStorageAccountStoreOptions(string accountName, DefaultAzureCredential credential)
    {
        Connection = new ConnectionOptions(accountName, credential);
    }

    public ConnectionOptions Connection { get; }

    public static AzureStorageAccountStoreOptions Credentials(IConfigurationSettings settings)
    {
        var accountKey = settings.GetString(AccountKeySettingName);
        var accountName = settings.GetString(AccountNameSettingName);
        var parts = new[]
        {
            "DefaultEndpointsProtocol=https",
            "EndpointSuffix=core.windows.net",
            $"AccountName={accountName}",
            $"AccountKey={accountKey}"
        };
        var connectionString = accountKey.HasValue()
            ? parts.Join(";")
            : DefaultConnectionString;
        return new AzureStorageAccountStoreOptions(connectionString);
    }

    public static AzureStorageAccountStoreOptions CustomConnectionString(string connectionString)
    {
        return new AzureStorageAccountStoreOptions(connectionString);
    }

    public static AzureStorageAccountStoreOptions UserManagedIdentity(IConfigurationSettings settings)
    {
        var accountName = settings.GetString(AccountNameSettingName);
        var clientId = settings.GetString(ManagedIdentityClientIdSettingName);

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId
            });

        return new AzureStorageAccountStoreOptions(accountName, credential);
    }

    public class ConnectionOptions
    {
        public enum ConnectionType
        {
            Credentials,
            ManagedIdentity
        }

        public ConnectionOptions(string connectionString)
        {
            Type = ConnectionType.Credentials;
            ConnectionString = connectionString;
            AccountName = null;
            Credential = null;
        }

        public ConnectionOptions(string accountName, DefaultAzureCredential credential)
        {
            Type = ConnectionType.ManagedIdentity;
            ConnectionString = null;
            AccountName = accountName;
            Credential = credential;
        }

        public string? AccountName { get; }

        public string? ConnectionString { get; }

        public DefaultAzureCredential? Credential { get; }

        public ConnectionType Type { get; }
    }
}