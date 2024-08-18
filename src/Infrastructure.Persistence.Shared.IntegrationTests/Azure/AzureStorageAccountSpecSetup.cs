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
public class AzureStorageAccountSpecSetup : StoreSpecSetupBase, IDisposable
{
    public AzureStorageAccountSpecSetup()
    {
        QueueStore = AzureStorageAccountQueueStore.Create(NoOpRecorder.Instance, Settings);
        BlobStore = AzureStorageAccountBlobStore.Create(NoOpRecorder.Instance, Settings);
        AzuriteStorageEmulator.Start();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            AzuriteStorageEmulator.Shutdown();
        }
    }

    public IBlobStore BlobStore { get; }

    public IQueueStore QueueStore { get; }
}