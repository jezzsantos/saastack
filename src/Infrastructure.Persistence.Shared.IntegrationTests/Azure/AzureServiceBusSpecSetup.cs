using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[CollectionDefinition("AzureServiceBus", DisableParallelization = true)]
public class AzureServiceBusSpecs : ICollectionFixture<AzureServiceBusSpecSetup>;

[UsedImplicitly]
public class AzureServiceBusSpecSetup : StoreSpecSetupBase, IDisposable
{
    public AzureServiceBusSpecSetup()
    {
        MessageBusStore =
            AzureServiceBusStore.Create(NoOpRecorder.Instance, AzureServiceBusStoreOptions.Credentials(Settings));
        AzureServiceBusBase.InitializeAllTests();
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
            AzureServiceBusBase.CleanupAllTests();
        }
    }

    public IMessageBusStore MessageBusStore { get; }
}