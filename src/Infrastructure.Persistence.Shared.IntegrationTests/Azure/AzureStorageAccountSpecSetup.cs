using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using IntegrationTesting.Persistence.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[CollectionDefinition("AzureStorageAccount", DisableParallelization = true)]
public class AzureStorageAccountSpecs : ICollectionFixture<AzureStorageAccountSpecSetup>;

[UsedImplicitly]
public class AzureStorageAccountSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private readonly AzuriteStorageEmulator _azurite = new();

    public IBlobStore BlobStore { get; private set; } = null!;

    public IQueueStore QueueStore { get; private set; } = null!;

    public async Task DisposeAsync()
    {
        await _azurite.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();
        var connectionString = _azurite.GetConnectionString();
#if TESTINGONLY
        QueueStore = AzureStorageAccountQueueStore.Create(NoOpRecorder.Instance,
            AzureStorageAccountStoreOptions.CustomConnectionString(connectionString));
        BlobStore = AzureStorageAccountBlobStore.Create(NoOpRecorder.Instance,
            AzureStorageAccountStoreOptions.CustomConnectionString(connectionString));
#endif
    }
}