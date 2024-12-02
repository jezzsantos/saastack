# Build an adapter to a 3rd party service

## Why?

You have defined a port in the code to a 3rd party remote service that is outside of your codebase (likely in the cloud).

You now want to build and test an adapter to that 3rd party service.

## What is the mechanism?

An "external" adapter is what implements a "port" (an abstraction already defined in your code) that adds custom behavior that integrates directly with the 3rd party service.

For example, you may wish to send email using a 3rd party service like: Mailgun, or SendGrid/Twilio.

Or you may wish to store data in a new database technology, like: Azure SQL, MongoDB or DynamoDB.

## Where to start?

All "external" adapters are created in the `Infrastructure.Shared` project in the folder: `ApplicationServices/External`.

> Here you will find the other adapters to 3rd party services.

Automated unit and integration tests for the adapter will go in the `Infrastucture.Shared.UnitTests` and `Infrastucture.Shared.IntegrationTests` projects.

> Some integrations with 3rd party services require that you also build webhook APIs yourself to handle changes communicated by 3rd party systems. Those API's would likely be hosted in one of the ApiHost projects, in an existing subdomain, or in a new subdomain. The choice is dependent on the integration, and what subdomain consumes it.

Most of these adapters will require static configuration settings. Static configuration will live (with all the other 3rd party configuration settings) in the `appsettings.json` files of the ApiHost project of the subdomains that ultimately will use the adapter.

## Before you build the adapter

A word about design and testing goals. 3rd party adapters are unique integrations that require extra levels of testing to ensure that they operate as you expect and that they keep operating over time as things change.

Depending on the vendor of the 3rd party technology you are integrating with, you will want to be sure that changes that they make over time do not break your integration.

When changes happen by the vendor, you will want to verify that things still work the same against a live/sandbox system.

> Ideally, vendors won't make (breaking) changes that break your integrations, but it does happen in reality. There are other ways that happen that are non-breaking that occur over long periods of time, including: (1) they change the public interface that you were using and release a newer version. (2) They obsoleted a public interface and replaced it with something else that, at some point, they stopped supporting; (3) They shipped new SDKs and stopped supporting older integrations. (4) They fix defects in their SDKs that change the behavior of your integration.

When you upgrade your adapter (perhaps to use a new, updated SDK version), you will also want to be confident you have done that properly and have not broken your old integration with this newer version. Again, automated testing against a live/sandbox system will easily verify that.

Lastly, if this adapter is used by default in local development, you will not want to be sending remote calls over the internet to a live/sandbox service in the cloud. You will instead want to use a stub API so that the integration keeps working in a pre-programmed and limited way.

These are some of the things that make building an adapter to a 3rd party service unique, and that require extra work. But payoff in the long run.

## How to build the adapter

Start by creating a new `sealed class` in the `Infrastructure.Shared` project in the folder: `ApplicationServices/External`.

If the 3rd party service you are integrating with is a cloud service, then you are likely building a class of adapter called a `HttpServiceClient`. So use that suffix for the name of your class.

Implement the interface of the "port" you want to service, and implement the methods from that interface.

Next you will create a constructor. You will likely need to inject at least these dependencies:

* `IRecorder` - to do tracing of success and errors (logging)
* `IConfigurationSettings` - to read static configuration settings, specific to the current environment. They will live under the "ApplicationServices" section of the configuration.
* `IHttpClientFactory` - you may need this depending on whether you use a vendor SDK, or not (see next section).

### Use a vendor SDK

In many cases, you will likely be using a public SDK that has been provided by the vendor to access their service remotely using HTTP. These SDKS are usually available as NuGet packages, and you can add them to only the `Infrastructure.Shared` project.

> Warning: Make sure that you choose the Nuget package carefully, and only from a trusted publisher - ideally the vendor themselves. Check their developer documentation. If there is not one for C#.NET, then you might have to build one yourself.

If you do not wish to use an SDK from a vendor, you can easily build your own HTTP service client using the `IServiceClient` port and `ApiServiceClient` adapter.

> You can see an example of an adapter that does exactly that with the `GravatarHttpServiceClient`.

We recommend that you do not inject instances from the vendor's SDK into your class, but instead only inject the dependencies that it requires to create those types in your adapter. Otherwise you are coupling the vendors SDK to your modules, which should be avoided if possible.

A note about testability.

Not only will you ultimately want to integration-test your adapter against the real 3rd party service (or a sandbox version of it) to ensure that it does as you designed it to do (against a real service in the cloud). You will also want unit-test it as well (particularly if it is simpler than the most trivial of all adapters)

If you are using a 3rd party SDK library, you will likely have trouble mocking the service client that your adapter will use from that library. If that is the case, we recommend that you build an abstraction of the service client from that library, which you can mock in unit testing.

This means that you need to do some extra work, but this will be worth it in the long run for a couple of reasons:

1. You can easily unit-test your adapter now, and not worry about HTTP concerns.
2. You can focus on the processing logic and mapping in your adapter (which most adapters will have).
3. Your custom `IServiceClient` implementation can focus on all aspects of logging and error handling when issuing remote HTTP calls, and you can keep this code separate from the logic and mapping code in your adapter.

> You can see several examples of custom service clients being used by other adapters in the `Infrastructure.Shared` project, particularly the `ChargebeeHttpServiceClient`, or `UserPilotHttpServiceClient`.

> Notice that the `FlagsmithHttpServiceClient` is an example of an adapter that is not using a custom service client abstraction, and you can also see that there are no unit tests for it either. Arguably it should have some unit tests as well, since it is not trivial code, and if it did, a custom service client would need to be built for it to be able to do that.

### Build custom service client

If you are going to build an abstraction around the service client of the vendor's library for testing purposes, then define an interface for the service client in another file along side your adapter class that is named like this: `MyAdapterHttpServiceClient.VendorClient.cs`.

In that file, add your interface called: `IVendorClient`, and create a derived `internal sealed class VendorClient : IVendorClient`

In this `VendorClient` class, create a new constructor, and inject the following dependencies:

* `IRecorder recorder`, `IConfigurationSettings settings` and `IHttpClientFactory httpClientFactory`

Create an `internal` constructor, and inject the following:

* `IRecorder recorder`, `IServiceClient serviceClient` and `IAsyncPolicy retryPolicy`

From this constructor, set fields of the class with the values.

Create a `private` constructor, and inject the following:

* `IRecorder recorder`, `string baseUrl`, `IAsyncPolicy retryPolicy` and `IHttpClientFactory httpClientFactory`

From the `private` constructor, call the `internal` constructor, and instantiate an instance of the `ApiServiceClient` class

From the `public` constructor, call the `private` constructor, and pass the `baseUrl` from `settings`, and create a retry policy from `ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter()`

For example, the `GravatarClient` looks like this,

```c#
internal class GravatarClient : IGravatarClient
{
    private const string BaseUrlSettingName = "ApplicationServices:Gravatar:BaseUrl";
    private readonly IRecorder _recorder;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IServiceClient _serviceClient;

    public GravatarClient(IRecorder recorder, IConfigurationSettings settings, IHttpClientFactory httpClientFactory)
        : this(recorder, settings.GetString(BaseUrlSettingName),
            ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(), httpClientFactory)
    {
    }

    internal GravatarClient(IRecorder recorder, IServiceClient serviceClient, IAsyncPolicy retryPolicy)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _retryPolicy = retryPolicy;
    }

    private GravatarClient(IRecorder recorder, string baseUrl, IAsyncPolicy retryPolicy,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new ApiServiceClient(httpClientFactory, JsonSerializerOptions.Default, baseUrl), retryPolicy)
    {
    }

    ... other methods
}
```

Now, back in your adapter class, create an `internal` constructor, that takes the following dependencies:

* `IRecorder recorder`, and `IVendorClient serviceClient`

From this constructor, set fields of the class with the values.

In the `public` constructor, call this `internal` constructor, and instantiate an instance of the `VendorClient` class using the other dependencies, and pass this instance into the `internal` constructor.

For example, the `GravatarHttpServiceClient` class looks like this,

```c#
public sealed class GravatarHttpServiceClient : IAvatarService
{
    private readonly IRecorder _recorder;
    private readonly IGravatarClient _serviceClient;

    public GravatarHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new GravatarClient(recorder, settings, httpClientFactory))
    {
    }

    internal GravatarHttpServiceClient(IRecorder recorder, IGravatarClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    ... other methods
}
```

> You can now use the `internal` constructors for unit testing, and the `public` constructors can be used at runtime.

### Build your adapter

Now you can implement your methods of the port, and make your requests through your custom `IVendorClient` interface.

## How to test the adapter

### Unit testing

Write your unit tests in the `Infrastructure.Shared.UnitTests` project, and verify the behavior of your adapter.

> Unit tests are always marked with the `Category=Unit` trait, and these tests are run very frequently and in every build.

You should test all public methods of your adapter (according to the "port" you have implemented). You are aiming for 100% coverage here.

If you have implemented a custom vendor client, you should also unit test parts of that adapter to ensure that requests are being prepared in the correct ways.

> Many vendors have very strict and specific ways to construct requests to their APIs, and handle specific error responses, and these constraints should be tested here.

### Integration testing

There are two kinds of integration testing that both need to be done for a 3rd party service adapter.

1. External integration tests (used to test the adapter directly)
2. API integration tests (where the adapter might be injected into the subdomains being tested)

#### External testing

These kinds of integration tests test the adapter directly against a live/sandboxed 3rd party service, whether that service is running on the local machine (i.e, in a docker container), or running in the cloud behind an HTTP API.

Write your "external" integration tests in the `Infrastructure.Shared.IntegrationTests` project, and verify the behavior of your adapter against real 3rd party systems (locally or in the cloud).

Please take note. Integration testing 3rd party adapters is different from other kinds of integration tests in this codebase.

Here are some reasons why:

1. These integration tests may (but not always) require infrastructure to be installed on your local machine (i.e. database servers, docker images etc.). If not that, then they will require live/sandboxed systems (in the cloud) to test against, for proper verification.
2. Many of these integrations will require some real configuration settings (i.e., connection strings, and credentials) to give your tests access to real live systems (local or in the cloud). This configuration may be sensitive, but should never compromise your production systems, even if they are exposed in your source code. Needless to say, production configuration secrets should never be saved in source code - ever! However, if you are using separate credentials and sandboxed environments for testing, then you can save these configuration settings in your source code in `appsettings.json` files. But be aware that sometimes this is not permitted either. Then you will need to use secret files locally only.
3. These integration tests will be expensive to run (in time) since you will need to be prepopulating your test data (in those 3rd party systems) before each test is run (or once before the test run). You will also need to be cleaning up previous test data (from previous test runs) before your test runs. This will make these tests run far slower than other kinds of tests.
4. When your adapter is functionally complete, and all tests pass, it will be unlikely that your tests will fail in the coming hours and days (as long as nothing changes in your code). We recommend always running these tests whenever changing the code in the adapter. However, these tests will need to be run frequently enough that you can detect changes with the 3rd party systems that you are testing against change. We recommend running them on the frequency of once a week, normally (perhaps in a scheduled CI build).
5. Some of the cloud systems and sandbox environments you will test against, may have usage limits or charge small fees for use. You certainly do not want to be racking up those quotas and possible charges, by running your tests more often than needed.

These are just some of the reasons why these integration tests should be tagged with the `Category=Integration.External` tests and need to be run infrequently.

##### Write your tests

Create a new test class in the `Infrastructure.Shared.IntegrationTests` project.

Mark up your class with the attributes: `[Trait("Category", "Integration.External")]` and `[Collection("EXTERNAL")]`.

> The
`[Collection("EXTERNAL")]` attribute is used to ensure that your tests do NOT run in parallel and share the same common setup and tear down methods.

Inherit from the `ExternalApiSpec` class, which will require you to inject the `ExternalApiSpec` instance into your constructor. This is also where you can get access to other DI dependencies for use in your tests.

Add any dependencies you want to be replaced during testing in the `OverrideDependencies()` method.

Lastly, add any configuration settings you might need in testing that are different that in local development, into the `appsettings.Testing.json` (or your own copy of the `appsettings.Testing.local.json`, which is not source controlled) file in the `Infrastructure.Shared.IntegrationTests` project.

> Please follow the same testing patterns that you see already in use in the tests of the other adapters in the same project.


For example,

```c#
[Trait("Category", "Integration.External")]
[Collection("EXTERNAL")]
public class MyCustomHttpServiceClientSpec : ExternalApiSpec
{
    private readonly MyCustomHttpServiceClient _serviceClient;
    private static bool _isInitialized;
    
    public MyCustomHttpServiceClientSpec(ExternalApiSetup setup) : base(setup, OverrideDependencies)
    {
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        _serviceClient = new MyCustomHttpServiceClient(NoOpRecorder.Instance, settings, new TestHttpClientFactory());
        if (!_isInitialized)
        {
            _isInitialized = true;
            SetupTestingSandboxAsync().GetAwaiter().GetResult();
        }
    }

    [Fact]
    public async Task WhenThen_Returns()
    {
        //... your test here
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //Add your dependency override here, like testing stubs 
    }
    
    private async Task SetupTestingSandboxAsync()
    {
#if TESTINGONLY
        //use your _serviceClient to destroy all data that could have been created by any previous tests
#endif
    }
}
```

#### API testing

These kinds of integration tests test the APIs provided by one of the subdomains of the product.

Depending on how your adapter is configured in the DI container when these tests run, determines whether your adapter is used at this time.
Generally, speaking, your adapter will be injected when the API integration tests are being run, and you will not want to be accessing a live/sandboxed 3rd party environment at this time, since these tests are run on every desktop very frequently. (see the notes above).

In this case, you will want to replace your adapter entirely with a stub adapter that does the bare minimum to respond to code that is dependent on it.

##### Build the stub adapter

Create a new test class in the `IntegrationTesting.WebApi.Common` project, in the `Stubs` folder.

Derive that class from the interface of the adapter.

> Name your stub class according to the interface, not the technology.

Implement the methods of that interface.

> For some adapters, you might want to allow tests to check its usage. If you do, provide some public getter properties that expose the data that could be fed to this interface, so that the API integration tests can ensure that the adapter was used in certain scenarios.

##### Register the stub adapter

In the `WebApiSetup<THost>` class of the `IntegrationTesting.WebApi.Common` project, inject your stub class in the `ConfigureTestServices()` handler of the `ConfigureWebHost()` method. (along with the others you see there).

> Note: this registration will override any registrations of your real adapter from DI code in the `ApiHost` modules and thus be used in all API integration testing.

## Stub your adapter in local development

If you register your new adapter via DI, and it is used (by default) in local (F5) development and debugging, and your adapter uses an HTTP service client to talk to a remote system in the cloud (for example). Then you will need to build a separate stub API for local development and debugging, so that your adapter still works locally.

> We need to avoid accessing real cloud systems in local (F5) development and debugging. So that all debugging can done offline, without requiring an internet connection. Also, we want to pre-program and stub how the real systems works in local development.


To do this, you will need to build a stub API that your adapter can talk to, that will return pre-programmed responses, and not actually talk to the cloud system.

You will also need to be able to control whether your adapter talks to your stub API, or talks to the real remote API by changing configuration in `appsettings.json`, or via some other clever tricks. For example using `#if TESTINGONLY` blocks of code.

> Note: Unfortunately, some vendor SDK will not let you change the base URL of the service client they use in their SDK. This is unfortunate and short-sighted of them, and a blocker for testing. This means you cannot easily get your adapter to point to your stub API during local development. Which leaves you with few other choices.
> One solution to this problem, is to not use the vendors SDK, and roll your own HTTP service client - which is not ideal, but not impossible either.

### Build your stub API

You will build your stub API in the `TestingStubApiHost` project in the `Api` folder.

You will need to add a `sealed class` that derives from `StubApiBase`, and that applies the `[BaseApiFrom("/vendor")]` attribute to it.

> Note: the `[BaseApiFrom]` attribute is required so that you can partition all inbound HTTP requests to the stub API they belong to, in cases where there are clashes with multiple vendors base URLs.

Next, and before you forget. Edit the value of the base URL setting that you might have put in the `appsettings.json` file of the `ApiHost` project, where you have registered your adapter using DI. Then, change the base URL value to point to your local stub API, so that your adapter will talk to your stub API in local development and debugging.

> Note: The `appsettings.json` values that you are defining here will also affect the API integration tests, of the subdomains that use your specific adapter. You should stub your adapters in those kinds of integration tests in the `OverrideDependencies()` method of those integration tests, rather than using your adapter in the tests.

For example, for Gravatar, the base URL in `appsettings.json` of the `ApiHost` project, will be set to `https://localhost:5656/gravatar`, and this setting will be used by your adapter in local (F5) development and debugging, so that your adapter will make requests to your stub API, and not the real service.

> When you deploy your CI build to production (or staging) environments, this specific base URL setting in `appsettings.json` would/should be replaced with something  like: `https://api.gravatar.com` instead. And that connects your adapter to the real world cloud service instead.

Now, you need to provide APIs for each of the endpoints that your adapter uses of the remote 3rd party service.

1. You will define these APIs and their respect request and response types the same way you normally build APIs for your subdomains. However, these request and response types should live in the `Infrastructure.Web.Api.Operations.Shared` project in the `3rdParties/Vendor` folder.
2. You will not need to specify any `[Authorize]` attributes on these request types, since those attributes won't be honoured by the Stub API project.
3. You may need to use JSON attributes like: `[JsonPropertyName("name")]`, on the properties of these request types, if the request require specific names, or casing (i.e. snake_casing).
4. Now implement your API methods. You'll need the exact same shape as those required by the 3rd party API since that will be what your adapter will be producing (using whichever service client library you are using). Use your vendor's docs for more details.
5. Make sure to use the `Recorder.TraceInformation()` method to output a trace that can be seen in the console of the running Stub Service. This is an important detail for debugging locally and seeing what's going on.
6. You will likely need to use in memory cached objects in your stub API, so that you can remember certain things to give meaningful responses. The goal here is to provide some pre-programmed behavior. It is not to try and accurately re-create the behavior of the real 3rd party service. However, some "memory" can be useful for normal operation.
7. Follow the same patterns as other stub APIs in the same location. Consistency is important.



