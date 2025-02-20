using Common.Recording;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.OnPremises.ApplicationServices;
using IntegrationTesting.Persistence.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.OnPremises;

[CollectionDefinition("RabbitMqQueue", DisableParallelization = true)]
public class RabbitMqQueueSpecs : ICollectionFixture<RabbitMqQueueSpecSetup>
{
}

[UsedImplicitly]
public class RabbitMqQueueSpecSetup : StoreSpecSetupBase, IDisposable
{
    private readonly RabbitMqEmulator _emulator;

    public RabbitMqQueueSpecSetup()
    {
        _emulator = new RabbitMqEmulator();
        _emulator.StartAsync().GetAwaiter().GetResult();

        var connectionString = _emulator.GetConnectionString();
        var managementUri = _emulator.GetManagementUri();

        var storeOptions = RabbitMqStoreOptions.FromConnectionString(connectionString);

        QueueStore = RabbitMqQueueStore.Create(NoOpRecorder.Instance, storeOptions);

        RabbitMqBase.InitializeAllTests();
    }

    public IQueueStore QueueStore { get; private set; } = null!;


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            RabbitMqBase.CleanupAllTests();
            _emulator.StopAsync().GetAwaiter().GetResult();
        }
    }
}
