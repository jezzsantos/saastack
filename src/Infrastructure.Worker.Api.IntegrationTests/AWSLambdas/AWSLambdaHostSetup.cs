using Common.Extensions;
using Common.Recording;
using Infrastructure.External.Persistence.AWS.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using IntegrationTesting.Persistence.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests.AWSLambdas;

[CollectionDefinition("AWSLambdas", DisableParallelization = true)]
public class AllAwsLambdaSpecs : ICollectionFixture<AWSLambdaHostSetup>;

[UsedImplicitly]
public class AWSLambdaHostSetup : IApiWorkerSpec, IAsyncLifetime
{
    private readonly AWSLocalStackEmulator _localStack = new();

    private static readonly TimeSpan LambdaTriggerWaitLatency = TimeSpan.FromSeconds(5);
    private IHost? _host;
    private Action<IServiceCollection>? _overridenTestingDependencies;

    public TService GetRequiredService<TService>()
        where TService : notnull
    {
        if (_host.NotExists())
        {
            throw new InvalidOperationException("Host has not be started yet!");
        }

        return _host.Services.GetRequiredService<TService>();
    }

    public IMessageBusStore MessageBusStore { get; private set; } = null!;

    public void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies)
    {
        _overridenTestingDependencies = overrideDependencies;
    }

    public IQueueStore QueueStore { get; private set; } = null!;

    public void StartHost()
    {
        _host = new HostBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder
                    .AddJsonFile("appsettings.Testing.json", true)
                    .AddJsonFile("appsettings.Testing.local.json", true);
            })
            .ConfigureServices((_, services) =>
            {
                if (_overridenTestingDependencies.Exists())
                {
                    _overridenTestingDependencies.Invoke(services);
                }
            })
            .Build();
        _host.Start();
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void WaitForQueueProcessingToComplete()
    {
        Thread.Sleep(LambdaTriggerWaitLatency);
    }

    public async Task InitializeAsync()
    {
        await _localStack.StartAsync();
        var recorder = NoOpRecorder.Instance;

#if TESTINGONLY
             var connectionString = _localStack.GetConnectionString();
        QueueStore = AWSSQSQueueStore.Create(recorder, connectionString);
        MessageBusStore =
            AWSSNSMessageBusStore.Create(recorder, new AWSSNSMessageBusStoreOptions(SubscriberType.Queue),
                connectionString);
#endif
    }

    public async Task DisposeAsync()
    {
        if (_host.Exists())
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await _localStack.StopAsync();
    }
}