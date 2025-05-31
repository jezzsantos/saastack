using Common.Configuration;
using EventStore.Client;

namespace Infrastructure.External.Persistence.Kurrent.ApplicationServices;

/// <summary>
///     Provides a singleton instance of <see cref="EventStoreClient" /> for use with the <see cref="KurrentEventStore" />.
/// </summary>
public class SharedKurrentEventStoreClient : IKurrentEventStoreClient, IDisposable
{
    public SharedKurrentEventStoreClient(IConfigurationSettings settings)
    {
        var connectionString = settings.GetString(KurrentEventStoreOptions.ConnectionStringSettingName);
        var clientSettings = EventStoreClientSettings.Create(connectionString);
        Client = new EventStoreClient(clientSettings);
    }

    public void Dispose()
    {
        Client.Dispose();
    }

    public EventStoreClient Client { get; }
}