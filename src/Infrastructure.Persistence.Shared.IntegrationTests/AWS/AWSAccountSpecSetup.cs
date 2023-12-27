using Common.Recording;
using Infrastructure.Persistence.AWS.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.AWS;

[CollectionDefinition("AWSAccount", DisableParallelization = true)]
public class AWSAccountSpecs : ICollectionFixture<AWSAccountSpecSetup>
{
}

[UsedImplicitly]
public class AWSAccountSpecSetup : StoreSpecSetupBase, IDisposable
{
    public AWSAccountSpecSetup()
    {
        QueueStore = AWSSQSQueueStore.Create(NullRecorder.Instance, Settings);
        AWSAccountBase.InitializeAllTests();
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
            AWSAccountBase.CleanupAllTests();
        }
    }

    public IQueueStore QueueStore { get; }
}