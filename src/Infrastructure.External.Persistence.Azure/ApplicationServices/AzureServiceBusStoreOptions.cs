using Azure.Identity;
using Common.Configuration;

namespace Infrastructure.External.Persistence.Azure.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="AzureServiceBusStore" />
/// </summary>
public class AzureServiceBusStoreOptions
{
    internal const string ConnectionStringSettingName =
        "ApplicationServices:Persistence:AzureServiceBus:ConnectionString";
    internal const string ManagedIdentityClientIdSettingName =
        "ApplicationServices:Persistence:AzureServiceBus:ManagedIdentityClientId";
    internal const string NamespaceNameSettingName =
        "ApplicationServices:Persistence:AzureServiceBus:NamespaceName";

    private AzureServiceBusStoreOptions(string connectionString)
    {
        Connection = new ConnectionOptions(connectionString);
    }

    private AzureServiceBusStoreOptions(string namespaceName, DefaultAzureCredential credential)
    {
        Connection = new ConnectionOptions(namespaceName, credential);
    }

    public ConnectionOptions Connection { get; }

    public static AzureServiceBusStoreOptions Credentials(IConfigurationSettings settings)
    {
        var connectionString = settings.GetString(ConnectionStringSettingName);
        return new AzureServiceBusStoreOptions(connectionString);
    }

    public static AzureServiceBusStoreOptions UserManagedIdentity(IConfigurationSettings settings)
    {
        var namespaceName = settings.GetString(NamespaceNameSettingName);
        var clientId = settings.GetString(ManagedIdentityClientIdSettingName);

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId
            });

        return new AzureServiceBusStoreOptions(namespaceName, credential);
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
            NamespaceName = null;
            Credential = null;
        }

        public ConnectionOptions(string namespaceName, DefaultAzureCredential credential)
        {
            Type = ConnectionType.ManagedIdentity;
            ConnectionString = null;
            NamespaceName = namespaceName;
            Credential = credential;
        }

        public string? ConnectionString { get; }

        public DefaultAzureCredential? Credential { get; }

        public string? NamespaceName { get; }

        public ConnectionType Type { get; }
    }
}