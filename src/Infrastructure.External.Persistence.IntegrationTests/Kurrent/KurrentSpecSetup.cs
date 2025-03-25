using Common.Recording;
using Infrastructure.External.Persistence.Kurrent.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Testcontainers.EventStoreDb;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.Kurrent;

[CollectionDefinition("Kurrent", DisableParallelization = true)]
public class AllKurrentSpecs : ICollectionFixture<KurrentSpecSetup>;

[UsedImplicitly]
public class KurrentSpecSetup : IAsyncLifetime
{
    private const string DockerImageName = "eventstore/eventstore:latest";
    // Docker image: https://hub.docker.com/r/eventstore/eventstore
    // Server config: https://developers.eventstore.com/server/v24.10/configuration
    private readonly EventStoreDbContainer _eventStoreDb = new EventStoreDbBuilder()
        .WithImage(DockerImageName)
        .WithEnvironment("EVENTSTORE_INSECURE", "true")
        .WithEnvironment("EVENTSTORE_MEM_DB", "true")
        .Build();

    public IEventStore EventStore { get; private set; } = null!;

    public async Task DisposeAsync()
    {
        await _eventStoreDb.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _eventStoreDb.StartAsync();

        var connectionString = _eventStoreDb.GetConnectionString();
        EventStore = KurrentEventStore.Create(NoOpRecorder.Instance, connectionString);
    }
}