using Common.Recording;
using FluentAssertions;
using Infrastructure.Persistence.AWS.ApplicationServices;
using JetBrains.Annotations;
using Testcontainers.LocalStack;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.AWS;

[CollectionDefinition("AWSAccount", DisableParallelization = true)]
public class AWSAccountSpecs : ICollectionFixture<AWSAccountSpecSetup>;

[UsedImplicitly]
public class AWSAccountSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private const string DockerImageName = "localstack/localstack:stable";

    private readonly LocalStackContainer _localStack = new LocalStackBuilder()
        .WithImage(DockerImageName)
        .Build();

    public AWSSNSMessageBusStore MessageBusStore { get; set; } = null!;

    public string[] MessageBusStoreTestQueues { get; set; } = null!;

    public AWSSQSQueueStore QueueStore { get; set; } = null!;

    public async Task InitializeAsync()
    {
        await _localStack.StartAsync();

#if TESTINGONLY
        QueueStore = AWSSQSQueueStore.Create(NoOpRecorder.Instance, _localStack.GetConnectionString());
        MessageBusStore = AWSSNSMessageBusStore.Create(NoOpRecorder.Instance, Settings,
            new AWSSNSMessageBusStoreOptions(SubscriberType.Queue), _localStack.GetConnectionString());
#endif
        var store = QueueStore.As<AWSSQSQueueStore>();
        var queue1 = store.CreateQueueAsync("messagebus_queue1", CancellationToken.None).GetAwaiter().GetResult();
        var queue2 = store.CreateQueueAsync("messagebus_queue2", CancellationToken.None).GetAwaiter().GetResult();
        MessageBusStoreTestQueues = [queue1.Value.QueueArn, queue2.Value.QueueArn];
    }

    public async Task DisposeAsync()
    {
        await _localStack.DisposeAsync();
    }
}