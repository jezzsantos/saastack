# Create a Subdomain Module

## Why?

You have a new aspect/capability/subdomain to add to the software, that may one day deployed independently, but right now is deployed with everything else.

## What is the mechanism?

A "subdomain" is realized (in a solution) as a collection of projects (usually about 8-10 projects).

A subdomain represents a "vertical slice" of the software, and is further divided into 3 horizontal layers: Infrastructure -> Application -> Domain, and includes (at the very least) both unit tests for each component within each horizontal layer, and integration tests for testing the public interfaces of the whole vertical slice (i.e. API).

## Where to start?

Often is the case that defining a subdomain (disparate from others), is the hardest part. The truth is that you should give it some thought, but at some point you have to accept that you may not get this right the first time around, and that means either combining it with another subdomain, or splitting it up into one or more other subdomains.

At the end of the day it is about the use-cases you have and the coupling with other subdomains that you want to design for.

> Note: You are aiming for reducing coupling while grouping use cases.

Every subdomain requires a name. Usually a noun (with a singular and plural form).

Technically, a subdomain is classified as a "Core" subdomain, or a "Generic" subdomain. A "core" subdomain is likely unique to your product, as opposed to a "Generic" subdomain which can be found in most other products.

Most subdomains will have at least one public interface. Often an API. Or it may also be other channels, like queues, buses or other.

The best way to get started with a new subdomain is to use the same patterns and assets used in existing subdomains.

> Note: We have included several project templates in this codebase to use to construct your subdomain. You will need to install them into your IDE first. See [Tooling](../design-principles/0140-developer-tooling.md) for instructions on doing that.

## Create the Infrastructure Projects

Generally speaking we would start by creating the API of the subdomain.

> Note: In some cases, you may start at the domain, and work inside-out from there. However, this approach does require a great deal of details to be known up front, and lots of focused time to track progress to complete it end to end. Whereas, starting at the API contract, and working outside-in, may sound counter-intuitive, but it tends to be easier to complete end to end, as you have several breadcrumbs to keep track pf your progress to completeness.

### Infrastructure Code

Infrastructure code is the code you need to write for the "infrastructure components" of your software. Essentially, infrastructure components are those that deal with any form of IO (input/output). (i.e. memory, disk, internet, database, etc.).

An API is infrastructure, driven by the internet (HTTP requests). All technology "adapters" (a.k.a ports-and-adapters) are also infrastructure components, whether they are driven by the Application Layer, or whether they drive the Application Layer.

Assuming our subdomain name is `House` (singular form).

All these components belong in this layer, in a project named: `HousesInfrastructure` (plural form).

#### The Module

The "module" is the unit of deployment, in a codebase like SaaStack.

* It usually contains one or more vertical slices of the software (Infrastructure + Application + Domain). It can be deployed standalone in some kind of runtime host (i.e. a web server or serverless container), or it can be deployed with one or more other modules in a shared host (i.e. a modular-monolith).

* The module is independent of other modules. It likely communicates with other modules via HTTP, (or other protocols like queues, messages buses etc). It can be independently tested in isolation or as part of a larger collection of modules.

* It defines it's own dependencies and configures itself within the host it is registered in. The host instructs the module on when to register its dependencies, and when to configure itself.

The module is a class in a file like `HousesModule.cs` (plural form). The class derives from `ISubdomainModule`, like this:

```c#

public class HousesModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(HousesModule).Assembly;

    public Assembly DomainAssembly => typeof(HouseRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(HouseRoot), "hse" }
    };

    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                // Any Application/Domain services that are consumed by the Application/Domain classes
                services.AddSingleton<IFoundationService, HouseFoundationService>();

                // The Application class
                services.AddPerHttpRequest<IHousesApplication>(c =>
                    new HousesApplication.HousesApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IFoundationService>(),
                        c.GetRequiredService<IHouseRepository>()));
                // The Repository class (event-sourcing)
                services.AddPerHttpRequest<IHouseRepository>(c =>
                    new HouseRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<HouseRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                // Wire-up the repository to eventing projections and notifications
                services.RegisterEventing<HouseRoot, HouseProjection, HouseNotifier>(c => 
                    new HouseProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    _ => new HouseNotifier());

                // Any Application Services exposed by this subdomain
                services.AddPerHttpRequest<IHousesService, HousesInProcessServiceClient>();
            };
        }
    }
}
```

##### Dependency Injection

Lets look at Dependency Injection and the rules around how you should configure the dependencies of your module. dependencies are injected, typically into constructors, and when injected at runtime, an instance of the dependency is used. Like most DI frameworks (we are based on .net > 8.0), there are three levels of instancing of dependencies:

1. **Singleton** - means one and only one instance of the dependency is created (ever) and reused whenever that dependency is resolved or injected into another class.
2. **Scoped** (a.k.a **PerHttpRequest**)- means one and only one instance of the dependency is created (per HTTP Request) and reused within the same HTTP thread. Other HTTP threads get their own instances. A unique instance per HTTP request.
3. **Transient** - means that a new instance of the dependency is created every time the dependency is resolved or injected into another class.

With these instancing "policies", it should be clear that if we instantiate a Singleton instance, and that class depends on an instance of another class that is a Transient, then that transient dependency is only ever injected once (ever) into the Singleton (at the moment that the Singleton is created). This is a grave mistake, and it may have difficult-to-debug side effects at runtime since the intended behavior of the Transient dependency has been altered by being injected into the Singleton instance.

> Note: This is an easy mistake to make in code at design time. Fortunately, many dependency injection frameworks (including .net8.0) detect this mistake at runtime and will throw an exception notifying the developer of their design-time mistake.

Thus, you should not inject a more volatile dependency into a less volatile dependency. So, it follows that the level of volatility of your dependency is governed by the dependents volatility levels.

Thus, if your class has a `Singleton` dependency injected into its constructor, then this class needs to be registered as `Singleton` (or `Scoped`, or `Transient`). If it has a `Scoped` dependency injected into its constructor, then it needs to be registered as `Scoped` (or `Transient`), and finally, if it has a `Transient` dependency injected into its constructor, then it can itself be registered as either `Singleton`, `Scoped` or `Transient`.

When you are registering the services of your module in the code above, you need to be aware of several underlying things. Let's look at a typical module, and how it wires up its dependencies:

1. Strictly speaking, your Application class (`HousesApplication`) should/could be registered as a `Singleton`, as long as it does not have any `Transient` or `Scoped` dependencies. However, if it has a `Scoped` Repository dependency (for example), then it also needs to be `Scoped`.
2. All "eventing" repositories, such as `IEventSourcingDddCommandStore<HouseRoot>` and `IEventSnapshottingDddCommandStore<HouseRoot>` (the two most common), must be registered as `Scoped` because:
   1. Reason 1: The eventing mechanism itself MUST be `Scoped` to each HTTP request; otherwise, we could be raising 'domain_events' multiple times - one for each concurrent HTTP request thread, subscribed to the `Singleton` producer of the events! which would be a serious disaster!
   2. Reason 2: For subdomains that are tenant-specific (as most "Core" subdomains will be), repositories (and some services) for that subdomain may use configuration specific to the specific tenant (i.e. connection strings). (see [MultiTenancy](../design-principles/0130-multitenancy.md) for more details on how), and thus, they must be registered as Scoped for that specific HTTP request. Otherwise, they could be sharing configuration. Another serious disaster!
   3. Because of the two reasons above, all of these eventing stores/repositories must be registered as `Scoped` (or `Transient`) but not `Singleton`. And, therefore, so must your `HousesApplication`.
3. Any services that your module defines (to be consumed by other modules in other subdomains), (e.g. `HousesInProcessServiceClient`) if and only if those services are in-process, then they will need to be `Scoped` because they inject the `housesApplication` as a dependency, which is also `Scoped`. If they are running out of process, they will be issuing HTTP calls to other subdomains, so they can probably be `Singleton`.
4. The only remaining services left are those injected into the `HousesApplication` (e.g. `HouseFoundationService`). They are being injected into a `Scoped` Application, so they can be either `Singleton` or `Scoped` depending on what dependencies they inject into themselves. `Singleton` would be preferred as a default.

#### The API

TBD

#### Persistence

TBD

#### Application Services

TBD

### Unit Tests

TBD

### Integration Tests

TBD

## Create the Application Projects

TBD

## Create the Domain Projects

TBD
