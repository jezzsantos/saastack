using Common.Recording;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.RabbitMq.ApplicationServices;
using IntegrationTesting.Persistence.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.RabbitMq;

[CollectionDefinition("RabbitMqMessageBus", DisableParallelization = true)]
public class RabbitMqMessageBusSpecs : ICollectionFixture<RabbitMqMessageBusSpecSetup>
{
}

[UsedImplicitly]
public class RabbitMqMessageBusSpecSetup : StoreSpecSetupBase, IDisposable
{
    private readonly RabbitMqEmulator _emulator;

    public RabbitMqMessageBusSpecSetup()
    {
        _emulator = new RabbitMqEmulator();
        _emulator.StartAsync().GetAwaiter().GetResult();

        var connectionString = _emulator.GetConnectionString();
        var managementUri = _emulator.GetManagementUri();

        var storeOptions = RabbitMqStoreOptions.FromConnectionString(connectionString);

        MessageBusStore = RabbitMqMessageBusStore.Create(NoOpRecorder.Instance, storeOptions);

        RabbitMqBase.InitializeAllTests();
    }

    public IMessageBusStore MessageBusStore { get; }

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
