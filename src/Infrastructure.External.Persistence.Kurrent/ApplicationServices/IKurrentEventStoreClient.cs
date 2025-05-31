using EventStore.Client;

namespace Infrastructure.External.Persistence.Kurrent.ApplicationServices;

public interface IKurrentEventStoreClient
{
    EventStoreClient Client { get; }
}