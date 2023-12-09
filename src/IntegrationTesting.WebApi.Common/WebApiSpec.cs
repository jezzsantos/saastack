using Application.Persistence.Interfaces;
using Common;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTesting.WebApi.Common;

/// <summary>
///     Provides an xUnit class fixture for integration testing APIs
/// </summary>
[UsedImplicitly]
public class WebApiSetup<THost> : WebApplicationFactory<THost>
    where THost : class
{
    private IServiceScope? _scope;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
        }

        base.Dispose(disposing);
    }

    private IConfiguration? Configuration { get; set; }

    public TInterface GetRequiredService<TInterface>()
        where TInterface : notnull
    {
        if (_scope is null)
        {
            _scope = Services.GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
        }

        return _scope.ServiceProvider.GetRequiredService<TInterface>();
    }

    public TInterface? TryGetService<TInterface>(Type serviceType)
    {
        if (_scope is null)
        {
            _scope = Services.GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
        }

        return (TInterface?)_scope.ServiceProvider.GetService(serviceType);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Testing.json", true)
                .Build();
            config.AddConfiguration(Configuration);
        });

        base.ConfigureWebHost(builder);
    }
}

/// <summary>
///     Provides an xUnit class fixture for integration testing APIs
/// </summary>
public abstract class WebApiSpec<THost> : IClassFixture<WebApiSetup<THost>>, IDisposable
    where THost : class
{
    // ReSharper disable once StaticMemberInGenericType
    private static IReadOnlyList<Type>? _allRepositories;

    // ReSharper disable once StaticMemberInGenericType
    private static IReadOnlyList<IApplicationRepository>? _repositories;
    protected readonly IHttpJsonClient Api;
    protected readonly HttpClient HttpApi;
    private readonly WebApplicationFactory<THost> _setup;

    protected WebApiSpec(WebApiSetup<THost> setup)
    {
        _setup = setup.WithWebHostBuilder(_ => { });

        HttpApi = setup.CreateClient();
        Api = new JsonClient(HttpApi);
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
            HttpApi.Dispose();
            _setup.Dispose();
        }
    }

    protected void EmptyAllRepositories(WebApiSetup<THost> setup)
    {
        var repositoryTypes = GetAllRepositoryTypes();
        var platformRepositories = GetRepositories(setup, repositoryTypes);

        DestroyAllRepositories(platformRepositories);
    }

    private static IReadOnlyList<IApplicationRepository> GetRepositories(WebApiSetup<THost> setup,
        IReadOnlyList<Type> repositoryTypes)
    {
        if (_repositories is null)
        {
            _repositories = repositoryTypes
                .Select(type => Try.Safely(() => setup.TryGetService<IApplicationRepository>(type)))
                .OfType<IApplicationRepository>()
                .ToList();
        }

        return _repositories;
    }

    private static IReadOnlyList<Type> GetAllRepositoryTypes()
    {
        if (_allRepositories is null)
        {
            _allRepositories = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes().Where(type =>
                    typeof(IApplicationRepository).IsAssignableFrom(type)
                    && type.IsInterface
                    && type != typeof(IApplicationRepository)))
                .ToList();
        }

        return _allRepositories;
    }

    private static void DestroyAllRepositories(IEnumerable<IApplicationRepository> repositories)
    {
        foreach (var repository in repositories)
        {
            repository.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}