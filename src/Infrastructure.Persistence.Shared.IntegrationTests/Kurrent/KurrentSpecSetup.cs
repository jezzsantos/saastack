using Common.Recording;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Kurrent.ApplicationServices;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

[CollectionDefinition("Kurrent", DisableParallelization = true)]
public class AllKurrentSpecs : ICollectionFixture<KurrentSpecSetup>;

[UsedImplicitly]
public class KurrentSpecSetup : StoreSpecSetupBase, IDisposable
{
    public KurrentSpecSetup()
    {
        EventStore = KurrentEventStore.Create(NoOpRecorder.Instance, Settings);
        KurrentBase.InitializeAllTests();
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
            KurrentBase.CleanupAllTests();
        }
    }

    public IEventStore EventStore { get; }
}