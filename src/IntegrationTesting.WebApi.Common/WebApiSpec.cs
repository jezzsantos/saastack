using System.Diagnostics;
using System.Text.Json;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Common.FeatureFlags;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common.Stubs;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using UnitTesting.Common;
using Xunit;

namespace IntegrationTesting.WebApi.Common;

/// <summary>
///     Provides an xUnit collection for running "Integration.API" tests together
/// </summary>
[CollectionDefinition("API", DisableParallelization = false)]
public class AllApiSpecs<THost> : ICollectionFixture<WebApiSetup<THost>>
    where THost : class;

/// <summary>
///     Provides an xUnit collection for running "Integration.Website" tests together
/// </summary>
[CollectionDefinition("WEBSITE", DisableParallelization = true)]
public class AllWebsiteSpecs<THost> : ICollectionFixture<WebApiSetup<THost>>
    where THost : class;

/// <summary>
///     Provides an xUnit class fixture for integration testing APIs
/// </summary>
[UsedImplicitly]
public class WebApiSetup<THost> : WebApplicationFactory<THost>
    where THost : class
{
    private Action<IServiceCollection>? _overridenTestingDependencies;
    private Action<WebApiSpec<THost>>? _runOnceAfterAllTests;
    private Action<WebApiSpec<THost>>? _runOnceBeforeAllTests;
    private WebApiSpec<THost>? _runOnceSpec;
    private IServiceScope? _scope;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_runOnceAfterAllTests.Exists())
            {
                _runOnceAfterAllTests(_runOnceSpec!);
            }

            _scope?.Dispose();
        }

        base.Dispose(disposing);
    }

    private IConfiguration? Configuration { get; set; }

    public TInterface GetRequiredService<TInterface>()
        where TInterface : notnull
    {
        if (_scope.NotExists())
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

    public void RunOnceForAllTests(Action<WebApiSpec<THost>> runOnceBeforeAllTests,
        Action<WebApiSpec<THost>>? runOnceAfterAllTests, WebApiSpec<THost> spec)
    {
        _runOnceSpec = spec;
        _runOnceBeforeAllTests = runOnceBeforeAllTests;
        _runOnceAfterAllTests = runOnceAfterAllTests;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration(config =>
            {
                Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.Testing.json", true)
                    .AddJsonFile("appsettings.Testing.local.json", true)
                    .Build();
                config.AddConfiguration(Configuration);
            })
            .ConfigureTestServices(services =>
            {
                //EXTEND: Add more stubs to 3rd party systems required by all tests 
                services.AddSingleton<IUserNotificationsService, StubUserNotificationsService>();
                services.AddSingleton<IFeatureFlags, StubFeatureFlags>();
                services.AddSingleton<IAvatarService, StubAvatarService>();
                services.AddSingleton<IUsageDeliveryService, StubUsageDeliveryService>();
                services.AddSingleton<IBillingProvider, StubBillingProvider>();
                if (_overridenTestingDependencies.Exists())
                {
                    _overridenTestingDependencies.Invoke(services);
                }
            });

        if (_runOnceBeforeAllTests.Exists())
        {
            _runOnceBeforeAllTests(_runOnceSpec!);
        }
    }
}

/// <summary>
///     Provides an xUnit class fixture for integration testing APIs.
///     Note: The HTTPClient instance used in tests is a special in-memory instance (by default at http://localhost)
///     must be used to call any API, since there is no TCP involved between HttpClient and the TestServer.
///     Note: Any call to any port using this HttpClient instance will reach the TestServer
///     Note: <see cref="TestingServerUrl" /> can be any port, since the client that is created, and used,
///     is not a real TCP HttpClient (<see cref="TestServer" /> docs)
/// </summary>
public abstract class WebApiSpec<THost> : IClassFixture<WebApiSetup<THost>>, IDisposable
    where THost : class
{
    protected const string PasswordForPerson = "1Password!";
    private const string DotNetCommandLineWithLaunchProfileArgumentsFormat =
        "run --no-build --configuration {0} --launch-profile {1} --project \"{2}\"";
    private const string TestingServerUrl = "https://localhost";
    private const int WaitStateRetries = 30;
    // ReSharper disable once StaticMemberInGenericType
    private static readonly TimeSpan WaitStateInterval = TimeSpan.FromSeconds(1);
    protected readonly IHttpJsonClient Api;
    protected readonly IHttpClient HttpApi;
    protected readonly StubUserNotificationsService UserNotificationsService;
    private readonly List<int> _additionalServerProcesses = [];
    private readonly WebApplicationFactory<THost> _setup;

    protected WebApiSpec(WebApiSetup<THost> setup, Action<IServiceCollection>? overrideDependencies = null,
        Action<WebApiSpec<THost>>? runOnceBeforeAllTests = null, Action<WebApiSpec<THost>>? runOnceAfterAllTests = null)
    {
        if (runOnceBeforeAllTests.Exists())
        {
            setup.RunOnceForAllTests(runOnceBeforeAllTests, runOnceAfterAllTests, this);
        }

        if (overrideDependencies.Exists())
        {
            setup.OverrideTestingDependencies(overrideDependencies);
        }

        _setup = setup.WithWebHostBuilder(_ => { });
        var clients = CreateTestingClients(setup);

        HttpApi = clients.HttpApi;
        Api = clients.Api;
        UserNotificationsService =
            setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
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
            (HttpApi as IDisposable)?.Dispose();
            _setup.Dispose();
        }
    }

    private static string DotNetExe
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return @"%ProgramFiles%\dotnet\dotnet.exe";
            }

            if (OperatingSystem.IsLinux())
            {
                return @"/usr/share/dotnet/dotnet";
            }

            if (OperatingSystem.IsMacOS())
            {
                return @"/usr/local/share/dotnet/dotnet";
            }

            throw new InvalidOperationException("Unsupported Platform");
        }
    }

    public void ShutdownAllAdditionalServers()
    {
        _additionalServerProcesses.ForEach(processId =>
        {
            if (processId != 0)
            {
                Try.Safely(() =>
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                });
            }
        });
    }

    public void StartupAdditionalServer<TAnotherHost>()
        where TAnotherHost : class
    {
        var assembly = typeof(TAnotherHost).Assembly;
        var projectName = assembly.GetName().Name!;
        var projectPath = Path.Combine(Solution.NavigateUpToSolutionDirectoryPath(), projectName);

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var launchProfileName = $"{projectName}-{env}";
        const string configuration = "Debug";
        var arguments =
            DotNetCommandLineWithLaunchProfileArgumentsFormat.Format(configuration, launchProfileName, projectPath);
        var executable = Environment.ExpandEnvironmentVariables(DotNetExe);
        var process = Process.Start(new ProcessStartInfo
        {
            Arguments = arguments,
            FileName = executable,
            RedirectStandardError = false,
            RedirectStandardOutput = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        });
        if (process.NotExists())
        {
            throw new InvalidOperationException($"Failed to launch Server {projectName}");
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException($"Failed to launch Server {projectName}, failed to startup");
        }

        _additionalServerProcesses.Add(process.Id);
    }

    protected void EmptyAllRepositories()
    {
#if TESTINGONLY
        Api.PostAsync(new DestroyAllRepositoriesRequest()).GetAwaiter().GetResult();
#endif
    }

    protected Stream GetTestImage()
    {
        return TestResources.ResourceManager.GetStream("TestImage")!;
    }

    /// <summary>
    ///     Registers the user and authenticates them
    /// </summary>
    protected async Task<LoginDetails> LoginUserAsync(LoginUser who = LoginUser.PersonA)
    {
        var emailAddress = GetEmailForPerson(who);
        var firstName = who switch
        {
            LoginUser.PersonA => "persona",
            LoginUser.PersonB => "personb",
            LoginUser.Operator => "operator",
            _ => throw new ArgumentOutOfRangeException(nameof(who), who, null)
        };

        var person = await RegisterUserAsync(emailAddress, firstName);

        var user = person.Credential.User;
        return await ReAuthenticateUserAsync(user, emailAddress);
    }

    /// <summary>
    ///     Manually propagates domain_events that are queued and waiting to be processed on the message bus.
    ///     Flows: <see href="../docs/images/Eventing-Flows-Generic.png" />
    /// </summary>
    protected async Task PropagateDomainEventsAsync(PropagationRounds rounds = PropagationRounds.Once)
    {
#if TESTINGONLY
        await Repeat.TimesAsync(async () =>
        {
            var request = new DrainAllDomainEventsRequest();
            await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));
        }, (int)rounds);
#endif
    }

    protected async Task<LoginDetails> ReAuthenticateUserAsync(LoginDetails details,
        string password = PasswordForPerson)
    {
        return await ReAuthenticateUserAsync(details.User, details.Profile!.EmailAddress!, password);
    }

    protected async Task<LoginDetails> ReAuthenticateUserAsync(EndUser user,
        string emailAddress, string password = PasswordForPerson)
    {
        await PropagateDomainEventsAsync();
        var login = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = emailAddress,
            Password = password
        });

        var accessToken = login.Content.Value.Tokens.AccessToken.Value;
        var refreshToken = login.Content.Value.Tokens.RefreshToken.Value;

        var profile = (await Api.GetAsync(new GetProfileForCallerRequest(),
            req => req.SetJWTBearerToken(accessToken))).Content.Value.Profile;

        var defaultOrganizationId = profile.DefaultOrganizationId!;
        return new LoginDetails(accessToken, refreshToken, user, profile, defaultOrganizationId);
    }

    protected async Task<RegisterPersonPasswordResponse> RegisterUserAsync(string emailAddress,
        string firstName = "afirstname", string lastName = "alastname")
    {
        var person = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            Password = PasswordForPerson,
            TermsAndConditionsAccepted = true
        });

        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonPasswordRequest
        {
            Token = token!
        });

        await PropagateDomainEventsAsync();

        return person.Content.Value;
    }

    /// <summary>
    ///     We retry the specified <see cref="request" />  until the <see cref="predicate" /> of the response is achieved
    /// </summary>
    protected async Task<JsonResponse<TResponse>> WaitForGetStateAsync<TResponse>(
        Predicate<JsonResponse<TResponse>> predicate,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? filter = null)
        where TResponse : IWebResponse, new()
    {
        var retryPolicy = Policy
            .HandleResult<JsonResponse<TResponse>>(res => !predicate(res))
            .WaitAndRetryAsync(WaitStateRetries, _ => WaitStateInterval);

        return await retryPolicy.ExecuteAsync(async () => await Api.GetAsync(request, filter));
    }

    private static string GetEmailForPerson(LoginUser who)
    {
        return who switch
        {
            LoginUser.PersonA => "person.a@company.com",
            LoginUser.PersonB => "person.b@company.com",
            LoginUser.Operator => "operator@company.com",
            _ => throw new ArgumentOutOfRangeException(nameof(who), who, null)
        };
    }

    private static (IHttpJsonClient Api, IHttpClient HttpApi) CreateTestingClients<TAnotherHost>(
        WebApiSetup<TAnotherHost> host)
        where TAnotherHost : class
    {
        var uri = new Uri(TestingServerUrl.WithoutTrailingSlash());
        var handler = new CookieContainerHandler();
        var client = host.CreateDefaultClient(uri, handler);

        var jsonOptions = host.GetRequiredService<JsonSerializerOptions>();
        var httpApi = new TestingClient(client, jsonOptions, handler);
        var api = new JsonClient(client, jsonOptions);

        return (api, httpApi);
    }

    protected class LoginDetails
    {
        public LoginDetails(string accessToken, string refreshToken, EndUser user, UserProfile? profile,
            string? defaultOrganizationId)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            User = user;
            Profile = profile;
            DefaultOrganizationId = defaultOrganizationId;
        }

        public string AccessToken { get; }

        public string? DefaultOrganizationId { get; }

        public UserProfile? Profile { get; }

        public string RefreshToken { get; set; }

        public EndUser User { get; }
    }

    protected enum LoginUser
    {
        PersonA = 0,
        PersonB = 1,
        Operator = 2
    }

    protected enum PropagationRounds
    {
        Once = 1,
        Twice = 2
    }
}