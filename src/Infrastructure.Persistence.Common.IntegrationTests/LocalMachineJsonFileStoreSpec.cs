#if TESTINGONLY
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Infrastructure.Persistence.Common.IntegrationTests;

[CollectionDefinition("LocalMachineJsonFileStore", DisableParallelization = true)]
public class AllLocalMachineJsonFileStoreSpecs : ICollectionFixture<LocalMachineJsonFileStoreSpecSetup>
{
}

[UsedImplicitly]
public class LocalMachineJsonFileStoreSpecSetup
{
    public LocalMachineJsonFileStoreSpecSetup()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile(@"appsettings.json").Build();
        var settings = new AspNetConfigurationSettings(configuration).Platform;
        DataStore = LocalMachineJsonFileStore.Create(settings);
        BlobStore = LocalMachineJsonFileStore.Create(settings);
        QueueStore = LocalMachineJsonFileStore.Create(settings);
        EventStore = LocalMachineJsonFileStore.Create(settings);
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