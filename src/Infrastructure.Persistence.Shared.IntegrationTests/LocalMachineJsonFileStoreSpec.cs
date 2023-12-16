#if TESTINGONLY
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

[CollectionDefinition("LocalMachineJsonFileStore", DisableParallelization = true)]
public class AllLocalMachineJsonFileStoreSpecs : ICollectionFixture<LocalMachineJsonFileStoreSpecSetup>
{
}

[UsedImplicitly]
public class LocalMachineJsonFileStoreSpecSetup : StoreSpecSetupBase
{
    public LocalMachineJsonFileStoreSpecSetup()
    {
        DataStore = LocalMachineJsonFileStore.Create(Settings);
        BlobStore = LocalMachineJsonFileStore.Create(Settings);
        QueueStore = LocalMachineJsonFileStore.Create(Settings);
        EventStore = LocalMachineJsonFileStore.Create(Settings);
    }

    public IBlobStore BlobStore { get; }

    public IDataStore DataStore { get; }

    public IEventStore EventStore { get; }

    public IQueueStore QueueStore { get; }
}

[Trait("Category", "Integration.Storage")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileDataStoreSpec : AnyDataStoreBaseSpec
{
    public LocalMachineJsonFileDataStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.DataStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileBlobStoreSpec : AnyBlobStoreBaseSpec
{
    public LocalMachineJsonFileBlobStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.BlobStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileQueueStoreSpec : AnyQueueStoreBaseSpec
{
    public LocalMachineJsonFileQueueStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.QueueStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("LocalMachineJsonFileStore")]
[UsedImplicitly]
public class LocalMachineJsonFileEventStoreSpec : AnyEventStoreBaseSpec
{
    public LocalMachineJsonFileEventStoreSpec(LocalMachineJsonFileStoreSpecSetup setup) : base(setup.EventStore)
    {
    }
}
#endif