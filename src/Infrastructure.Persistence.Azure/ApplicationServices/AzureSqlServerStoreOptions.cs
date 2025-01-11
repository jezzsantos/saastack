using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Persistence.Azure.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="AzureSqlServerStore" />
/// </summary>
public class AzureSqlServerStoreOptions
{
    internal const string DbCredentialsSettingName = "ApplicationServices:Persistence:SqlServer:DbCredentials";
    internal const string DbNameSettingName = "ApplicationServices:Persistence:SqlServer:DbName";
    internal const string DbServerNameSettingName = "ApplicationServices:Persistence:SqlServer:DbServerName";
    internal const string ManagedIdentityClientIdSettingName = "ApplicationServices:Persistence:SqlServer:ManagedIdentityClientId";

    private AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType connectionType, string connectionString)
    {
        Connection = new ConnectionOptions(connectionType, connectionString);
    }

    public ConnectionOptions Connection { get; }

    public static AzureSqlServerStoreOptions Credentials(IConfigurationSettings settings)
    {
        var serverName = settings.GetString(DbServerNameSettingName);
        var databaseName = settings.GetString(DbNameSettingName);
        var credentials = settings.GetString(DbCredentialsSettingName, string.Empty);

        var parts = new[]
        {
            "Persist Security Info=False",
            credentials.HasValue()
                ? "Encrypt=True"
                : "Integrated Security=true;Encrypt=False",
            $"Initial Catalog={databaseName}",
            $"Server={serverName}",
            credentials.HasValue()
                ? credentials
                : string.Empty
        };
        var connectionString = parts.Join(";");
        return new AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType.Credentials, connectionString);
    }

    public static AzureSqlServerStoreOptions CustomConnectionString(string connectionString)
    {
        return new AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType.Custom, connectionString);
    }

    public static AzureSqlServerStoreOptions UserManagedIdentity(IConfigurationSettings settings)
    {
        var serverName = settings.GetString(DbServerNameSettingName);
        var databaseName = settings.GetString(DbNameSettingName);
        var clientId = settings.GetString(ManagedIdentityClientIdSettingName);

        var parts = new[]
        {
            $"Server={serverName}",
            "Authentication=Active Directory Managed Identity",
            "Encrypt=True",
            $"User Id={clientId}",
            $"Database={databaseName}"
        };
        var connectionString = parts.Join(";");
        return new AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType.ManagedIdentity, connectionString);
    }

    public class ConnectionOptions
    {
        public enum ConnectionType
        {
            Credentials,
            ManagedIdentity,
            Custom
        }

        public ConnectionOptions(ConnectionType type, string connectionString)
        {
            Type = type;
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public ConnectionType Type { get; }
    }
}