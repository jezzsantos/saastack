using Common.Configuration;
using Common.Extensions;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
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
public class AllExternalSpecs : ICollectionFixture<ExternalApiSetup>
{
}

/// <summary>
///     Provides an xUnit class fixture for external integration testing APIs
/// </summary>
[UsedImplicitly]
public class ExternalApiSetup : IDisposable
{
    private IHost? _host;
    private Action<IServiceCollection>? _overridenTestingDependencies;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_host.Exists())
            {
                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }
        }
    }

    public TService GetRequiredService<TService>()
        where TService : notnull
    {
        if (_host.NotExists())
        {
            throw new InvalidOperationException("Host has not be started yet!");
        }

        return _host.Services.Resolve<TService>();
    }

    public void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies)
    {
        _overridenTestingDependencies = overrideDependencies;
    }

    public void Start()
    {
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
    }
}

/// <summary>
///     Provides an xUnit class fixture for external integration testing APIs
/// </summary>
public abstract class ExternalApiSpec : IClassFixture<ExternalApiSetup>, IDisposable
{
    protected readonly ExternalApiSetup Setup;

    protected ExternalApiSpec(ExternalApiSetup setup, Action<IServiceCollection>? overrideDependencies = null)
    {
        if (overrideDependencies.Exists())
        {
            setup.OverrideTestingDependencies(overrideDependencies);
        }

        setup.Start();
        Setup = setup;
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
            if (Setup is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}