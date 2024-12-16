using Common.Recording;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Kurrent.ApplicationServices;
using JetBrains.Annotations;
using Testcontainers.EventStoreDb;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

[CollectionDefinition("Kurrent", DisableParallelization = true)]
public class AllKurrentSpecs : ICollectionFixture<KurrentSpecSetup>;

[UsedImplicitly]
public class KurrentSpecSetup : IAsyncLifetime
{
    private const string DockerImageName = "eventstore/eventstore:latest";

    // Can I reference the setting in KurrentEventStore ? 
    private const string EventStoreConnectionStringSettingName =
        "ApplicationServices:Persistence:EventStoreDb:ConnectionString";

    // Docker image: https://hub.docker.com/r/eventstore/eventstore
    // Server config: https://developers.eventstore.com/server/v24.10/configuration
    private readonly EventStoreDbContainer _eventStoreDb = new EventStoreDbBuilder()
        .WithImage(DockerImageName)
        // .WithReuse(true) // Uncomment to keep the container alive after the test  
        .WithPortBinding(2113, true)
        .WithEnvironment("EVENTSTORE_INSECURE", "true")
        .WithEnvironment("EVENTSTORE_MEM_DB", "true")
        .Build();

    public IEventStore EventStore { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _eventStoreDb.StartAsync();

        // Settings need to be dynamically configured with container values
        var settings = TestHelpers.CreateTestSettings(new Dictionary<string, string?>
        {
            { EventStoreConnectionStringSettingName, _eventStoreDb.GetConnectionString() }
        });

        EventStore = KurrentEventStore.Create(NoOpRecorder.Instance, settings);
    }

    public async Task DisposeAsync()
    {
        await _eventStoreDb.DisposeAsync();
    }
}