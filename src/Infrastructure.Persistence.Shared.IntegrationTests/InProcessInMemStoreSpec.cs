#if TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

[CollectionDefinition("InProcessInMemStore", DisableParallelization = true)]
public class AllInProcessInMemStoreSpecs : ICollectionFixture<InProcessInMemStoreSpecSetup>
{
}

[UsedImplicitly]
public class InProcessInMemStoreSpecSetup : StoreSpecSetupBase
{
    public IBlobStore BlobStore { get; } = new InProcessInMemStore();

    public IDataStore DataStore { get; } = new InProcessInMemStore();

    public IEventStore EventStore { get; } = new InProcessInMemStore();

    public IQueueStore QueueStore { get; } = new InProcessInMemStore();
}

[Trait("Category", "Integration.Storage")]
[Collection("InProcessInMemStore")]
[UsedImplicitly]
public class InProcessInMemDataStoreSpec : AnyDataStoreBaseSpec
{
    public InProcessInMemDataStoreSpec(InProcessInMemStoreSpecSetup setup) : base(setup.DataStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("InProcessInMemStore")]
[UsedImplicitly]
public class InProcessInMemBlobStoreSpec : AnyBlobStoreBaseSpec
{
    public InProcessInMemBlobStoreSpec(InProcessInMemStoreSpecSetup setup) : base(setup.BlobStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("InProcessInMemStore")]
[UsedImplicitly]
public class InProcessInMemQueueStoreSpec : AnyQueueStoreBaseSpec
{
    public InProcessInMemQueueStoreSpec(InProcessInMemStoreSpecSetup setup) : base(setup.QueueStore)
    {
    }
}

[Trait("Category", "Integration.Storage")]
[Collection("InProcessInMemStore")]
[UsedImplicitly]
public class InProcessInMemEventStoreSpec : AnyEventStoreBaseSpec
{
    public InProcessInMemEventStoreSpec(InProcessInMemStoreSpecSetup setup) : base(setup.EventStore)
    {
    }
}
#endif