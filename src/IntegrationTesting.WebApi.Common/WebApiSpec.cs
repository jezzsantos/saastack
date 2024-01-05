using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Application.Persistence.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;
using IntegrationTesting.WebApi.Common.Stubs;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
    private Action<IServiceCollection>? _overridenTestingDependencies;
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

    public void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies)
    {
        _overridenTestingDependencies = overrideDependencies;
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
        builder
            .ConfigureAppConfiguration(config =>
            {
                Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.Testing.json", true)
                    .Build();
                config.AddConfiguration(Configuration);
            })
            .ConfigureTestServices(services =>
            {
                services.AddSingleton<INotificationsService, StubNotificationsService>();
                if (_overridenTestingDependencies.Exists())
                {
                    _overridenTestingDependencies.Invoke(services);
                }
            });
    }
}

/// <summary>
///     Provides an xUnit class fixture for integration testing APIs
/// </summary>
public abstract class WebApiSpec<THost> : IClassFixture<WebApiSetup<THost>>, IDisposable
    where THost : class
{
    private const string WebServerBaseUrlFormat = "https://localhost:{0}/";
    // ReSharper disable once StaticMemberInGenericType
    private static IReadOnlyList<Type>? _allRepositories;

    // ReSharper disable once StaticMemberInGenericType
    private static IReadOnlyList<IApplicationRepository>? _repositories;
    protected readonly IHttpJsonClient Api;
    protected readonly HttpClient HttpApi;
    protected StubNotificationsService NotificationsService;
    private readonly WebApplicationFactory<THost> _setup;

    protected WebApiSpec(WebApiSetup<THost> setup, Action<IServiceCollection>? overrideDependencies = null)
    {
        if (overrideDependencies.Exists())
        {
            setup.OverrideTestingDependencies(overrideDependencies);
        }

        _setup = setup.WithWebHostBuilder(_ => { });

        var jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
        HttpApi = setup.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(WebServerBaseUrlFormat.Format(GetNextAvailablePort()))
        });
        Api = new JsonClient(HttpApi, jsonOptions);
        NotificationsService = setup.GetRequiredService<INotificationsService>().As<StubNotificationsService>();
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

    protected async Task<LoginUser> LoginUserAsync()
    {
        const string emailAddress = "aperson1@company.com";
        const string password = "1Password!";
        var person = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            EmailAddress = emailAddress,
            FirstName = "aperson1",
            LastName = "alastname",
            Password = password,
            TermsAndConditionsAccepted = true
        });

        var token = NotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonPasswordRequest
        {
            Token = token!
        });

        var login = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = emailAddress,
            Password = password
        });

        return new LoginUser(login.Content.Value.AccessToken!, person.Content.Value.Credential!.User);
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

    private static int GetNextAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    protected class LoginUser
    {
        public LoginUser(string accessToken, RegisteredEndUser user)
        {
            AccessToken = accessToken;
            User = user;
        }

        public string AccessToken { get; }

        public RegisteredEndUser User { get; }
    }
}