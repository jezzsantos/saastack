using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
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
        QueueStore = AzureStorageAccountQueueStore.Create(NullRecorder.Instance, Settings);
        AzureStorageAccountBase.InitializeAllTests();
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
            AzureStorageAccountBase.CleanupAllTests();
        }
    }

    public IQueueStore QueueStore { get; }
}