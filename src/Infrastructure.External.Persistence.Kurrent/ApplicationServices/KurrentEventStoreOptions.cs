using Common.Configuration;
using EventStore.Client;

namespace Infrastructure.External.Persistence.Kurrent.ApplicationServices;

/// <summary>
///     Defines the options for creating and <see cref="KurrentEventStore" />
/// </summary>
public class KurrentEventStoreOptions : IDisposable
{
    internal const string ConnectionStringSettingName = "ApplicationServices:Persistence:Kurrent:ConnectionString";

    private KurrentEventStoreOptions(ConnectionOptions.ConnectionType connectionType, EventStoreClient client,
        bool ownsClient)
    {
        Connection = new ConnectionOptions(connectionType, client, ownsClient);
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    public ConnectionOptions Connection { get; }

    public static KurrentEventStoreOptions ConnectionString(IConfigurationSettings settings)
    {
        var connectionString = settings.GetString(ConnectionStringSettingName);
        return CustomConnectionString(connectionString);
    }

    public static KurrentEventStoreOptions CustomConnectionString(string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);

        return new KurrentEventStoreOptions(ConnectionOptions.ConnectionType.Custom, new EventStoreClient(settings),
            true);
    }

    /// <summary>
    ///     Returns a shared client.
    ///     Kurrent.io recommends sharing a singleton instance of <see cref="EventStoreClient" />.
    ///     See: <see href="https://docs.kurrent.io/clients/grpc/getting-started.html#creating-a-client" />
    /// </summary>
    public static KurrentEventStoreOptions SharedClient(IKurrentEventStoreClient client)
    {
        return new KurrentEventStoreOptions(ConnectionOptions.ConnectionType.SharedClient, client.Client, false);
    }

    public class ConnectionOptions : IDisposable
    {
        public enum ConnectionType
        {
            SharedClient,
            Custom
        }

        private readonly bool _ownsClient;

        public ConnectionOptions(ConnectionType type, EventStoreClient client, bool ownsClient)
        {
            Type = type;
            Client = client;
            _ownsClient = ownsClient;
        }

        public void Dispose()
        {
            if (_ownsClient)
            {
                Client.Dispose();
            }
        }

        public EventStoreClient Client { get; }

        public ConnectionType Type { get; }
    }
}