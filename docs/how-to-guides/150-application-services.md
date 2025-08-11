# Application Services

## Why?

In a modular monolith or microservices architecture, where you have more than one vertical slice, the subdomains within each vertical slice might need to communicate with each other to fulfil business requirements. 

However, direct coupling between subdomains violates the principles of vertical slice architecture and Domain-Driven Design and worse, it creates tight dependencies that make the system harder to maintain, test, deploy and scale.

Application Services solve this problem by providing a clean abstraction layer for cross-subdomain communication. They enable:

- **Loose coupling** between vertical slices and subdomains, with a well-defined "contract"
- **Deployment flexibility** - the same code on either side the "contract" can continue to work whether subdomains are deployed together in the same deployment host (in-process) or when they are separately deployed (over HTTP)
- **Testability** - easy to mock and test cross-subdomain interactions, without exposing the implementation details within the application service 

All Application services are consumed by classes in the Application Layer. They are typically used in the Transactional scripts of the classes in the Application Layer, who use these services, typically for either storing or retrieving data or both.

These "coordinator" transactional scripts should NOT be implementing code that directly accesses data from any infrastructural source (i.e., databases, file systems, internet, or even memory caches etc.). That kind of code should be encapsulated and abstracted behind an Application Service contract.

Application services are the poster child of ports and adapters as the primary means to plug and play, and decouple components across the whole system.

Application services are used for many common purposes:

* All data Repositories (all `IApplicationRepository` derivatives) are in fact technically "Application Services", they are simply a special case of an application service and very common variant that most engineers are familiar with - which is why they are defined in a special sub-folder called `Persistence`
* All technology ports/adapters are "Application Services". Whether you want to send an email, or delegate a call to a 3rd party Identity Server, or encrypt a token value.
* Cross-Domain ports/adapters, are used to access functionality in other vertical slices or subdomains.

## What is the mechanism?

All Application Services in SaaStack are defined (as ports, as C# interfaces) in an "Application Layer", but they are typically implemented (as adapters, as C# classes) in the "Infrastructure layer".

> Many adapters involve some kind of infrastructure, usually some kind of I/O of some type or connecting to remote systems over HTTP. e.g., accessing data through repositories, exchange information over the internet via SDKs, accessing configuration files, etc. 
>
> However, it is also very possible that some application services are just logic running in memory, consuming other application services, and thus their implementations may then exist in an "Application layer" project also.

### Location

Depending on the scope of use of the application service (i.e., who consumes it), they are either defined:

1. If the application service is only consumed by a single subdomain, both port and adapter are defined the same subdomain as the consumer of the service.
   - The port will always be defined in the "Application" project, and the adapter in either the "Infrastructure" project, or in the "Application" project of the same subdomain where they are consumed.
   - Both port and adapter will be in code in a sub folder called "Application Services" in their respective projects.
   - For example, the port `IOAuth2ClientService` is defined in the `IdentityApplication` project and the adapter `NativeOAuth2ClientService` just happens to also be defined in the `IdentityApplication` since it has no direct infrastructure needs, it just consumes other application services.
   - For example, the port `ISSOService` is defined in the `IdentityApplication` project and the adapter `SSOInProcessServiceClient` is defined in the `IdentityInfrastructure` project, since it will have infrastructure needs.
2. If the application service is consumed by many subdomains:
   1. The port is defined centrally in the `Application.Services.Shared` project, at the root of the project
   2. The adapter will be defined centrally in the `Infrastructure.Shared` project, in a sub folder caller `ApplicationServices`
   3. For example, the port `IEmailSchedulingService` is defined in the `Application.Services.Shared` project, while the adapter `QueuingEmailSchedulingService` is defined in the `Infrastructure.Shared` project.

All Application services are injected at runtime. Typically, using the submodule registration method.

> However, a few widely used Application Services may be injected, for all deployed hosts, in the centralized host configuration in `HostExtensions.cs`. For example, `IEmailSchedulingService` 

## Where to start?

### Step 1: Design the interface

The interface or contract of an Application Service is called a "port", in port and adapters terminology.

The port is always defined as a C# interface, and it is recommended to add XML documentation especially to ports that are used outside a subdomain.

Application Services should be designed as high level abstractions, from the point of view of the Application code that consumes them. They should not be over-generalized as is common with building abstractions. Start with a use case that is very specific, and generalize later, if and when needed. 

> They should definitely NOT leak implementation details of the source of the data, or technique of obtaining that data, being either provided or consumed into the port definition.
>
> For example, a specific subdomain repository port does not leak (C# types nor language and terminology) that describes the fact that it might actually be coupled (at runtime in any particular deployment) to a specific technology like Microsoft SQL Server. The consumer should not care that the data is being fetched from a database. It simply does not care about such details or how to handle such interactions.

All Application Service ports MUST define their inputs and outputs in terms of resources found in the `Application.Resources.Shared` namespace.

* They MUST never use types defined by 3rd party SDKs, NuGet packages, or types defined in any Infrastructure Layer assembly. All types must be owned by this codebase.
* They SHOULD never use types defined in the Domain Layer, such as Value Objects.
  * Except in the case of repository ports (e.g, `ICarRepository`) that are very specific to a specific subdomain, where those definitions SHOULD use Value Objects and Aggregate roots as a shortcut of convenience to avoid another pointless mapping having to be defined.
* Data that flows outside a subdomain, MUST always be mapped to "resources" in the `Application.Resources.Shared` namespace. Or to types defined on the port itself (which is rarer). 
* Result type SHOULD always be used with an error (e.g., `Result<SomeResource, Error>`), and adapter implementations can still throw exceptions, but only for exceptional cases. Prefer errors.
* Nullable types (e.g., `DateTime?`) are recommended for inputs and outputs on signatures. Whereas `Optional<T>` types are permitted but not always appropriate.
* Only instance methods should be supported. No properties, static methods, etc.
  * The name of the C# interface and the name and types used in method signatures should tell the consumer at a glance what the code does behind the scenes. Use full names, to qualify the signature and its types.
* You SHOULD pass the `ICallerContext` as the first parameter to most methods. You SHOULD NOT pass the `IRecorder`.
* You SHOULD declare the return type `async Task<Result<Resource, Error>>`, and suffix the method name with `Async` especially if the adapter accesses any I/O, and you should pass a `CancellationToken` as the last parameter.
* Optionally, add brief XML summary comments to the interface, and all its methods, but your methods should be self-describing to begin with.

#### Repository ports

Repository ports are a special variant of Application Services that have their own specific design and implementation details, that are highly consistent right across the codebase. Please see other existing declarations for examples of this consistency, for example `ICarRepository`.

* DO derive the port from `IApplicationRepository`. This is required for correct clean-up during testing.
* DO define the name of the port as the name of the subdomain, singularized, plus the suffix `Repository`. e.g., `ICarRepository`
* DO define the return type as a `async Task<Result<ASomething, Error>>` where `ASomething` is either a root aggregate type or a read model type, and the method is suffixed with `Async` and accepts a `CancellationToken` as the last parameter.
* DO define the input variables of the signatures of methods using ONLY primitive types, or value objects, or aggregate types.
* DO define the output variables of the signatures of methods using ONLY either aggregate types or read model types.
* DO use `Optional<T>` and not nullable types for either input or output variables.
* Do NOT pass the `ICallerContext` in this interface, nor `IRecorder`.
* Do NOT add documentation to this definition

#### Cross-Domain ports

Cross domain ports are also a special variant of Application Services that have a very consistent pattern and implementation details, that are highly consistent right across the codebase. Please see other existing declarations for examples of this consistency, for example `ICarsApplication`

* DO define the name of the port as the name of the subdomain, pluralised, plus the suffix `Service`. e.g., `ICarsService`
* Do pass the `ICallerContext` as first parameters of all methods.
* DO define the return type as a `async Task<Result<AResource, Error>>`, and suffix the method name with `Async` and you should pass a `CancellationToken` as the last parameter.
* DO use either primitives or resource types for input or output types. DO NOT use any domain types or any infrastructure types.
* DO prefer use of nullable types for inputs and outputs. `Optional<T>` types permitted.
* DO define methods that are considered "private" APIs with a suffix of `PrivateAsync` to indicate that these will not be public APIs in the future.
* DO NOT add documentation to this definition

### Step 2: Create the port in the right location

Is the port being used within a single subdomain?

* Yes? -> add the interface to an `ApplicationServices` subfolder of the Application project within the only subdomain that will consume it.
  * Repositories:  Are always single subdomain, and MUST live in the `Persistence` subfolder of the Application project within the only subdomain that will consume it.
* No? -> add the interface to the `Application.Services.Shared` project.
  * Cross-Domain: are always shared

### Step 3: Create an adapter in the right location

The adapter or implementation of an Application Service is called an "adapter", in port and adapters terminology.

The adapter is always defined as a C# class, which implements the port defined in the previous step.

Is the adapter being used in a single subdomain?

* Yes? -> add the interface to an `ApplicationServices` subfolder of the Infrastructure project within the only subdomain that will consume it.
  * Repositories: Are always single subdomain.
* No? -> add the interface to the `ApplicationServices` subfolder of the  `Infrastructure.Shared` project.
  * Cross-domain: are always shared

### Step 4: Design the adapter

The adapter class SHOULD use constructor injection to inject any dependencies. e.g., `IRecorder`, and `IConfigurationSettings` for technology adapters, or some application layer class like `ICarsApplication` for cross domain services.

> The lifetime of the class instance will be determined by how it is injected into consumers at runtime, in the next step.



At this early stage, you do NOT need to implement the adapter to move forward, however you do need a class defined in the code for the next step.

>  We recommend that you simply implement all methods as a skeleton and either throw a `NotImplmentedException()` or return a `Result.Ok` result. And leave your self a `//TODO: must implement` reminder.



When you do get around to implementing the class, unless it is an in-process cross-domain service, you MUST write at least unit tests for each method to test your implementation logic. (Unit tests for in-process cross-domain services are low value in almost all cases).

For some adapters, particularly technology adapters, you MUST implement external integration tests (`category=Integration.External`) as well as unit tests, to test the adapter against 3rd party systems, when you release it into a product deployment. See the [Building Third Party Adapters](100-build-adapter-third-party.md) for more details on those additional steps.

Feel free to use 3rd party SDKs in this adapter, or other I/O and infrastructure packages, and only add those NuGet packages to the project where the adapter is created.

#### Repository adapters

For consistency, please use the identical patterns you find in all repository implementations.

See `ICarRepository` as a complete representation of a typical repository for event-sourced aggregates, and `IBookingRepository` as a complete representation of a repository for snapshotting aggregates.

#### Cross-Domain adapters

Cross-domain service adapters may come in two flavors of implementation, depending on how their module is deployed physically.

They are either deployed as:

* In-process service clients, when exposing subdomains that are running in the same host (i.e., in-process in the same executable)
* Or they are deployed as HTTP service clients, when exposing the subdomain for access from another deployed host (i.e., accessed via HTTP)

You will need at least one of the following implemented for your specific deployment.  

##### In-Process Service Clients

When subdomains are deployed in the same host, in-process service clients provide direct access to the Application Layer

Thus, they can simply delegate calls to the Application class of the subdomain. This is designed to be highly convenient for the implementor, and saves additional unnecessary mapping code.

For example:

````csharp
public class CarsInProcessServiceClient : ICarsService
{
    private readonly ICarsApplication _carsApplication;

    public CarsInProcessServiceClient(ICarsApplication carsApplication)
    {
        _carsApplication = carsApplication;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        return await _carsApplication.GetCarAsync(caller, organizationId, id, cancellationToken);
    }
````

##### HTTP Service Clients

When subdomains are deployed in different hosts, HTTP service clients provide access via an HTTP service client

Thus, they need to translate the call to an HTTP request and marshal that across the internet.

For example:

````csharp
public class CarsHttpServiceClient : ICarsService
{
    private readonly IServiceClient _serviceClient;

    public CarsHttpServiceClient(IServiceClient serviceClient)
    {
        _serviceClient = serviceClient;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        var response = await _serviceClient.GetAsync(caller, new GetCarRequest
        {
            OrganizationId = organizationId,
            Id = id
        }, null, cancellationToken);

        return response.Match<Result<Car, Error>>(res => res.Value.Car, error => error.ToError());
    }
````

### Step 5: Register the adapter in dependency injection

To use the Application Service at runtime, it must be injected by dependency injection into the consuming class. This will be done, by injecting the port.

You first need to decide whether the port and adapter belong to a specific subdomain, or whether they are to be made available to all subdomains.

#### Shared services

In the later case, the registration of the port and adapter is likely best done in the `HostExtensions.cs` class that is used by all deployed hosts.

In this case you can use `HostOptions` to control which hosts to inject the dependency into.
You also need to determine which function within `HostExtensions` class to use that best describes the port.

For example, you could register it as a dependency in `RegisterSharedServices()` method (as a default), or in any one of the other methods defined within the `ConfigureApiHost()` method if it fits better there. 

#### Subdomain specific services

In the `ISubdomainModule` class of your subdomain that will consume this port, register both the port and adapter in the `RegisterServices` method.

Be careful to determine whether the lifetime of the instance you are injecting is a Singleton instance, or more commonly a Scoped instance, for which we use the method `AddPerHttpRequest`.

> Be aware of the scope of the consuming service. If that consuming service is scoped (i.e., registered with `AddPerHttpRequest()`), then your application must either be registered with `AddPerHttpRequest()` or `AddSingleton()`. But if the consuming service was registered with `AddSingleton()`, then your application service can only be registered a `AddSingleton()`.
>
> You will receive a runtime error if you fail to comply with these limitations.

Furthermore, you can also register your adapter in a standard and easy way relaying on a public default constructor to use dependencies already in the container. Or you can get very specific about which dependencies are injected using the factory registration methods - however this is generally not recommended for most simple adapters.

```c#
public Action<ConfigurationManager, IServiceCollection> RegisterServices
{
    get
    {
        return (_, services) =>
        {
            // Other registrations...

            services.AddPerHttpRequest<IMyApplicationService, MyApplicationService>();

            // services.AddPerHttpRequest<IMyApplicationService>(c =>
            //     new MyApplicationService(
            //         c.GetRequiredService<ISomething>()
            //             .CreateSomething("https://mysubdomain-api.example.com")));
        };
    }
}
```

### Step 6: (Optional) stub out during integration testing

Since your new adapter is likely being injected at runtime when integration testing the APIs of your subdomain, you may want to replace its default behavior during integration testing.

This is not always the case, and not always problem. It depends on the kind of application service you are building.

You can skip this consideration if you are building any of these kinds of application services:

* A repository for your subdomain
* A cross-domain service client
* A service that delegates to other application services.

Essentially, most common application services except technology adapters, which may need special treatment during integration testing.

In these cases, follow the guidance for [Building Third Party Adapters](100-build-adapter-third-party.md) for more details on how to build a stub for testing. 