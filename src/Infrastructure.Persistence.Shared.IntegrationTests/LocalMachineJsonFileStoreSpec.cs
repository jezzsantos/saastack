#if TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

[CollectionDefinition("LocalMachineJsonFileStore", DisableParallelization = true)]
public class AllLocalMachineJsonFileStoreSpecs : ICollectionFixture<LocalMachineJsonFileStoreSpecSetup>;

[UsedImplicitly]
public class LocalMachineJsonFileStoreSpecSetup : StoreSpecSetupBase
{
    private readonly LocalMachineJsonFileStore _store;

    public LocalMachineJsonFileStoreSpecSetup()
    {
        _store = LocalMachineJsonFileStore.Create(Settings);
    }

    public IBlobStore BlobStore => _store;

    public IDataStore DataStore => _store;

    public IEventStore EventStore => _store;

    public IMessageBusStore MessageBusStore => _store;

    public IQueueStore QueueStore => _store;
}

[Trait("Category", "Integration.Persistence")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileDataStoreSpec : AnyDataStoreBaseSpec
{
    public LocalMachineJsonFileDataStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.DataStore)
    {
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileBlobStoreSpec : AnyBlobStoreBaseSpec
{
    public LocalMachineJsonFileBlobStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.BlobStore)
    {
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileQueueStoreSpec : AnyQueueStoreBaseSpec
{
    public LocalMachineJsonFileQueueStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.QueueStore)
    {
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileEventStoreSpec : AnyEventStoreBaseSpec
{
    public LocalMachineJsonFileEventStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.EventStore)
    {
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileMessageBusStoreSpec : AnyMessageBusStoreBaseSpec
{
    public LocalMachineJsonFileMessageBusStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(
        setup.MessageBusStore)
    {
    }
}

#endif