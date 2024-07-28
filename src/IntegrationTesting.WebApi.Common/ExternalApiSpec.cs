using Common.Configuration;
using Common.Extensions;
using Infrastructure.Hosting.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IntegrationTesting.WebApi.Common;

/// <summary>
///     Provides an xUnit collection for running "External" tests together
/// </summary>
[CollectionDefinition("External", DisableParallelization = false)]
public class AllExternalSpecs : ICollectionFixture<ExternalApiSetup>;

/// <summary>
///     Provides an xUnit class fixture for external integration testing APIs
/// </summary>
[UsedImplicitly]
public class ExternalApiSetup : IDisposable
{
    private IHost? _host;
    private bool _isStarted;
    private Action<IServiceCollection>? _overridenTestingDependencies;
    private Action<ExternalApiSpec>? _runOnceAfterAllTests;
    private Action<ExternalApiSpec>? _runOnceBeforeAllTests;
    private ExternalApiSpec? _runOnceSpec;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_runOnceAfterAllTests.Exists())
            {
                _runOnceAfterAllTests(_runOnceSpec!);
            }

            if (_host.Exists())
            {
                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }
        }
    }

    public void EnsureStarted()
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;

        _host = new HostBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder
                    .AddJsonFile("appsettings.Testing.json", true)
                    .AddJsonFile("appsettings.Testing.local.json", true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfigurationSettings>(
                    new AspNetDynamicConfigurationSettings(context.Configuration));
                if (_overridenTestingDependencies.Exists())
                {
                    _overridenTestingDependencies.Invoke(services);
                }
            })
            .Build();
        _host.Start();

        if (_runOnceBeforeAllTests.Exists())
        {
            _runOnceBeforeAllTests(_runOnceSpec!);
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

    public void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies)
    {
        _overridenTestingDependencies = overrideDependencies;
    }

    public void RunOnceForAllTests(Action<ExternalApiSpec> runOnceBeforeAllTests,
        Action<ExternalApiSpec>? runOnceAfterAllTests, ExternalApiSpec spec)
    {
        _runOnceSpec = spec;
        _runOnceBeforeAllTests = runOnceBeforeAllTests;
        _runOnceAfterAllTests = runOnceAfterAllTests;
    }
}

/// <summary>
///     Provides an xUnit class fixture for external integration testing APIs
/// </summary>
public abstract class ExternalApiSpec : IClassFixture<ExternalApiSetup>
{
    protected ExternalApiSpec(ExternalApiSetup setup, Action<IServiceCollection>? overrideDependencies = null,
        Action<ExternalApiSpec>? runOnceBeforeAllTests = null, Action<ExternalApiSpec>? runOnceAfterAllTests = null)
    {
        if (runOnceBeforeAllTests.Exists())
        {
            setup.RunOnceForAllTests(runOnceBeforeAllTests, runOnceAfterAllTests, this);
        }

        if (overrideDependencies.Exists())
        {
            setup.OverrideTestingDependencies(overrideDependencies);
        }

        setup.EnsureStarted();
    }
}