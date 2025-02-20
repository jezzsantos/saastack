using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Persistence.OnPremises.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="AzureSqlServerStore" />
/// </summary>
public class SqlServerStoreOptions
{
    internal const string DbCredentialsFormatSettingName = "ApplicationServices:Persistence:{0}:DbCredentials";
    internal const string DbNameFormatSettingName = "ApplicationServices:Persistence:{0}:DbName";
    internal const string DbServerNameFormatSettingName = "ApplicationServices:Persistence:{0}:DbServerName";
    private const string DefaultPrefix = "SqlServer";
    internal const string ManagedIdentityClientIdFormatSettingName =
        "ApplicationServices:Persistence:{0}:ManagedIdentityClientId";

    private SqlServerStoreOptions(ConnectionOptions.ConnectionType connectionType, string connectionString)
    {
        Connection = new ConnectionOptions(connectionType, connectionString);
    }

    public ConnectionOptions Connection { get; }

    public static SqlServerStoreOptions AlternativeCredentials(IConfigurationSettings settings, string prefix)
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
        return new SqlServerStoreOptions(ConnectionOptions.ConnectionType.Credentials, connectionString);
    }

    public static SqlServerStoreOptions Credentials(IConfigurationSettings settings)
    {
        return AlternativeCredentials(settings, DefaultPrefix);
    }

    public static SqlServerStoreOptions CustomConnectionString(string connectionString)
    {
        return new SqlServerStoreOptions(ConnectionOptions.ConnectionType.Custom, connectionString);
    }

    public static SqlServerStoreOptions UserManagedIdentity(IConfigurationSettings settings)
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
        return new SqlServerStoreOptions(ConnectionOptions.ConnectionType.ManagedIdentity, connectionString);
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