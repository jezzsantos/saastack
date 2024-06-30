using Common.Recording;
using FluentAssertions;
using Infrastructure.Persistence.AWS.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.AWS;

[CollectionDefinition("AWSAccount", DisableParallelization = true)]
public class AWSAccountSpecs : ICollectionFixture<AWSAccountSpecSetup>;

[UsedImplicitly]
public class AWSAccountSpecSetup : StoreSpecSetupBase, IDisposable
{
    public AWSAccountSpecSetup()
    {
        QueueStore = AWSSQSQueueStore.Create(NoOpRecorder.Instance, Settings);
        MessageBusStore = AWSSNSMessageBusStore.Create(NoOpRecorder.Instance, Settings,
            new AWSSNSMessageBusStoreOptions(SubscriberType.Queue));
        AWSAccountBase.InitializeAllTests();

        var store = QueueStore.As<AWSSQSQueueStore>();
        var queue1 = store.CreateQueueAsync("messagebus_queue1", CancellationToken.None).GetAwaiter().GetResult();
        var queue2 = store.CreateQueueAsync("messagebus_queue2", CancellationToken.None).GetAwaiter().GetResult();
        MessageBusStoreTestQueues = [queue1.Value.QueueArn, queue2.Value.QueueArn];
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

    public IMessageBusStore MessageBusStore { get; }

    public string[] MessageBusStoreTestQueues { get; }

    public IQueueStore QueueStore { get; }
}