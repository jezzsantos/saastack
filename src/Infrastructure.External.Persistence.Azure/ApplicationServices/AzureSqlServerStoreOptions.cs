using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.External.Persistence.Azure.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="AzureSqlServerStore" />
/// </summary>
public class AzureSqlServerStoreOptions
{
    internal const string DbCredentialsFormatSettingName = "ApplicationServices:Persistence:{0}:DbCredentials";
    internal const string DbNameFormatSettingName = "ApplicationServices:Persistence:{0}:DbName";
    internal const string DbServerNameFormatSettingName = "ApplicationServices:Persistence:{0}:DbServerName";
    private const string DefaultPrefix = "SqlServer";
    internal const string ManagedIdentityClientIdFormatSettingName =
        "ApplicationServices:Persistence:{0}:ManagedIdentityClientId";

    private AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType connectionType, string connectionString)
    {
        Connection = new ConnectionOptions(connectionType, connectionString);
    }

    public ConnectionOptions Connection { get; }

    public static AzureSqlServerStoreOptions AlternativeCredentials(IConfigurationSettings settings, string prefix)
    {
        var serverName = settings.GetString(DbServerNameFormatSettingName.Format(prefix));
        var databaseName = settings.GetString(DbNameFormatSettingName.Format(prefix));
        var credentials = settings.GetString(DbCredentialsFormatSettingName.Format(prefix), string.Empty);

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

    public static AzureSqlServerStoreOptions Credentials(IConfigurationSettings settings)
    {
        return AlternativeCredentials(settings, DefaultPrefix);
    }

    public static AzureSqlServerStoreOptions CustomConnectionString(string connectionString)
    {
        return new AzureSqlServerStoreOptions(ConnectionOptions.ConnectionType.Custom, connectionString);
    }

    public static AzureSqlServerStoreOptions UserManagedIdentity(IConfigurationSettings settings)
    {
        var serverName = settings.GetString(DbServerNameFormatSettingName.Format(DefaultPrefix));
        var databaseName = settings.GetString(DbNameFormatSettingName.Format(DefaultPrefix));
        var clientId = settings.GetString(ManagedIdentityClientIdFormatSettingName.Format(DefaultPrefix));

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