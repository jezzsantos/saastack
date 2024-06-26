using Common.Extensions;
using Common.Recording;
using Infrastructure.Hosting.Common;
using Infrastructure.Persistence.AWS.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests.AWSLambdas;

[CollectionDefinition("AWSLambdas", DisableParallelization = true)]
public class AllAwsLambdaSpecs : ICollectionFixture<AWSLambdaHostSetup>;

[UsedImplicitly]
public class AWSLambdaHostSetup : IApiWorkerSpec, IDisposable
{
    private static readonly TimeSpan LambdaTriggerWaitLatency = TimeSpan.FromSeconds(5);
    private IHost? _host;
    private Action<IServiceCollection>? _overridenTestingDependencies;

    public AWSLambdaHostSetup()
    {
        var settings = new AspNetDynamicConfigurationSettings(new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json", true)
            .AddJsonFile("appsettings.Testing.local.json", true)
            .Build());
        var recorder = NoOpRecorder.Instance;
        QueueStore = AWSSQSQueueStore.Create(recorder, settings);
        MessageBusStore =
            AWSSNSMessageBusStore.Create(recorder, settings, new AWSSNSMessageBusStoreOptions(SubscriberType.Queue));
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
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            AWSAccountBase.CleanupAllTests();
        }
    }

    public TService GetRequiredService<TService>()
        where TService : notnull
    {
        if (_host.NotExists())
        {
            throw new InvalidOperationException("Host has not be started yet!");
        }

        return _host.Services.GetRequiredService<TService>();
    }

    public IMessageBusStore MessageBusStore { get; }

    public void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies)
    {
        _overridenTestingDependencies = overrideDependencies;
    }

    public IQueueStore QueueStore { get; }

    public void Start()
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
}