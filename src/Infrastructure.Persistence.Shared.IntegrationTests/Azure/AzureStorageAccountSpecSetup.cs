using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Testcontainers.Azurite;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[CollectionDefinition("AzureStorageAccount", DisableParallelization = true)]
public class AzureStorageAccountSpecs : ICollectionFixture<AzureStorageAccountSpecSetup>;


[UsedImplicitly]
public class AzureStorageAccountSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private const string DockerImageName = "mcr.microsoft.com/azure-storage/azurite:latest";

    private readonly AzuriteContainer _azurite = new AzuriteBuilder()
        .WithImage(DockerImageName)
        .WithInMemoryPersistence()
        .Build();

    public IBlobStore BlobStore { get; private set; } = null!;

    public IQueueStore QueueStore { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();
        var connectionString = _azurite.GetConnectionString();
#if TESTINGONLY
        QueueStore = AzureStorageAccountQueueStore.Create(NoOpRecorder.Instance, connectionString);
        BlobStore = AzureStorageAccountBlobStore.Create(NoOpRecorder.Instance, connectionString);
#endif
    }

    public async Task DisposeAsync()
    {
        await _azurite.DisposeAsync();
    }
}