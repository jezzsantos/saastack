using Common.Recording;
using FluentAssertions;
using Infrastructure.Persistence.AWS.ApplicationServices;
using IntegrationTesting.Persistence.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.AWS;

[CollectionDefinition("AWSAccount", DisableParallelization = true)]
public class AWSAccountSpecs : ICollectionFixture<AWSAccountSpecSetup>;

[UsedImplicitly]
public class AWSAccountSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private readonly AWSLocalStackEmulator _localStack = new();

    public AWSSNSMessageBusStore MessageBusStore { get; private set; } = null!;

    public string[] MessageBusStoreTestQueues { get; private set; } = null!;

    public AWSSQSQueueStore QueueStore { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _localStack.StartAsync();

#if TESTINGONLY
        var connectionString = _localStack.GetConnectionString();
        QueueStore = AWSSQSQueueStore.Create(NoOpRecorder.Instance, connectionString);
        MessageBusStore = AWSSNSMessageBusStore.Create(NoOpRecorder.Instance,
            new AWSSNSMessageBusStoreOptions(SubscriberType.Queue), connectionString);
#endif
        var store = QueueStore.As<AWSSQSQueueStore>();
        var queue1 = store.CreateQueueAsync("messagebus_queue1", CancellationToken.None).GetAwaiter().GetResult();
        var queue2 = store.CreateQueueAsync("messagebus_queue2", CancellationToken.None).GetAwaiter().GetResult();
        MessageBusStoreTestQueues = [queue1.Value.QueueArn, queue2.Value.QueueArn];
    }

    public async Task DisposeAsync()
    {
        await _localStack.StopAsync();
    }
}