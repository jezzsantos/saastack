# Modularity

How should we construct and define our modules (so that we can develop/test them together), and how should we deploy them into a staging/production environment?

## Design Principles

1. While we are a small team (e.g., a single product team or several product teams working on the same release cadence), for as long as possible, we want to be working on the same codebase so that its whole design can evolve as a single unit. This is one case of using a ["Mono-Repo"](https://en.wikipedia.org/wiki/Monorepo).
2. We want to (as much as possible) run, test, and debug the entire system or the most relevant sub-system of the system.
3. As a business [survives and] grows, the complexity of the software naturally increases as the software supports more functionality and as the subdomain models are explored in deeper detail. More use cases crop up (i.e., more features), and the demand and load on the software starts to increase (i.e., more users/growth). Due to either of these causes (or all of them), typically, the performance of the software decreases as more and more components/modules come into existence and more and more need to interact with each other. Bottlenecks on shared infrastructure are common. To improve that performance, the components/modules of the system are required to be split and scaled independently (e.g., towards independent microservices, with dedicated infrastructure). Eventually, it will become necessary to split the system components in the backend to serve more clients in the frontend. (Note: Typically, frontends are only split by channel or audience, whereas backends tend to be split by subdomain or by load/demand). We do not want to have to re-engineer the system to split it up; the mechanism to split it up should already be in place and can require additional work to connect the split pieces together once split. This is precisely the case for using a ["Modular Monolith"](../decisions/0010-deployment-model.md).
4. We've [already decided](../decisions/0045-modularization.md) that we will structure the backend into subdomains (and use DDD structures/practices), and those will be the base unit of our modules.
5. The modules/subdomains in the system at present are defined using the `ISubDomainModule` abstraction and collated into individual Host projects (e.g, web projects or executables) using `SubDomainModules`, which are then packaged and deployed onto respective hosts/infrastructure in the cloud (e.g., into an "App Service" in Azure, or into a "Lambda" or "EC2 instance" in AWS). When they are deployed in the same deployed Host (i.e., into the same running process), then they can communicate with each other (through ports and adapters) with "in-process" adapters. When they are deployed into separate Hosts/infrastructure, they must communicate across HTTP, using the same port, but using a different HTTP adapter. Thus, even though these submodules can be deployed and scaled independently (to improve performance and reduce bottlenecks), the additional communication between them now increases latency and decreases reliability between them. See the [CAP Theorem](https://en.wikipedia.org/wiki/CAP_theorem) for the introduced challenges this brings.
6. While this codebase starts off with defining and deploying 1 single Backend API and 1 single Website, running alongside numerous other infrastructure components (i.e, queues, functions/lambdas, databases, etc), it is anticipated that at some time in the future, that more client applications will be built for this specific product (e.g, more websites or more mobile apps, or appliances, etc) and that the API will be split into several API Hosts (for salability), and then routed together using some kind of API/Application Gateway to appear as a single IP address.

![Modular Monolith](../images/Physical-Architecture-Azure.png)

## Implementation

### A Backend API Host

At present, we define and deploy 1 single `ApiHost1` project.

On Azure, this would be [typically] deployed to a single "Azure App Service". On AWS, this could be deployed as a Lambda, or as an EC2 instance.

> In both cloud providers, there are other options for deploying this `ApiHost1` project, for example, in Docker Containers, Kubernetes, etc. These would require additional development for your product.

### A Module

A logical "module" in this codebase is intended to be independently deployable, and is composed of several parts:

1. The code for a module is defined in an `ISubDomainModule`, which is then collated into a `SubDomainModules` in a physical host project like `ApiHost1.HostedModules`. All extensibility and dependency injection, related to this module, is declared in the `ISubDomainModule`
2. The host project (i.e., `ApiHost`) configures itself by defining its `WebHostOptions`, which in turn drives the configuration of the ASPNET process, and pipelines (via the call to `WebApplicationBuilder.ConfigureApiHost` in `Program.cs`). All dependency injection and setup for the host is contained in call to `ConfigureApiHost`.
3. Configuration settings are declared in the host project, in a collection of `appsettings.json` files, some that are environment-specific and others that are host-specific, respective to the components in the module.
4. Data that the subdomain "owns" (or originates) is intentionally organized separately, from other subdomains, right down to how it is stored in their respective repositories (e.g., event stores, databases, caches, etc).

> Note: When it comes to data repositories like relational databases (where joining data is a native capability), traditionally, developers working in a monolith are used to creating joins/dependencies between tables across the whole schema. This entangles the individual tables in larger database, making splitting modules later an extremely hard, if not impossible, task. Thus, with a Modular Monolith, extra special care has to be taken not to just reuse tables across individual subdomains but to keep them entirely separate (possibly duplicating some data). So that those tables pertaining to a single module can be either moved (or copied) to other database deployments without breaking the system or the integrity of the data.
>
> Note: To be a micro-service, you must be able to maintain and control your own data in a separate infrastructure, from other micro-services.

### Splitting API Hosts

> How to split up Modular Monolith into Deployable Hosts?

Later in the development of your SaaS product, it will become necessary to split the code and data of your modular monolith, possibly due to performance/complexity issues, possibly due to a change in your organization (e.g., [Conway's Law](https://en.wikipedia.org/wiki/Conway%27s_law)), possibly due to a change in strategy.

Whatever the case is, you will need to split up the code, data, and services of your single API Host `ApiHost1`.

> Warning: There are several subdomains that work very, very closely together (and depend on each other to operate well), and should not be split readily. For example, the subdomains `EndUser`, `Organization,` and `Profile` subdomains form the core of the "multi-tenancy" capability. Splitting these subdomains can be done technically, but it is assumed that it cannot be done without incurring significant performance degradation (albeit that assumption has not been done nor been proven).

#### Creating a new Host

To make this easier, here are the steps you will need to split `ApiHost1` into another host, called `ApiHost2`:

1. Create a new Host project called `ApiHost2`. Copy the entire project from `ApiHost1` and rename it to `ApiHost2`.

2. Files that you will need to change (in order):

   1. `Properties/launchsettings.json` -
      1. Rename all the tasks
      2. Assign a new `applicationUrl` (IP address and host) for both local, and production environments.
   2. Remove the `Api/TestingOnly` folder.
      1. Leave the `Api/Health` folder in place, but rename the type inside `HealthApi.cs`.
   3. Edit the `HostedModules.cs`,
      1. Remove any sub-modules that will NOT be hosted in this host.
      2. Remove the `TestingOnlyApiModule`
      3. Add any new modules for this host.
   4. Edit `Program.cs`,
      1. Change the namespace to `ApiHost2`
      2. Choose a different `WebHostOptions` option. You could consider using the `WebHostOptions.BackEndApiHost`, or define your own.
   5. Edit the `Resources.resx` file,
      1. Remove all settings for `GetTestingOnly*`.
      2. Consider deleting the file, if you have nothing to put in there.
   6. Delete the `tenantsettings.json` file, provided that you are NOT hosting the `OrganizationsModule` in this host.
   7. Delete the `TestingOnlyApiModule.cs`

3. Now that you know more about what modules you will actually host in this project, change these files (in order):

   1. Edit the `ApiHostModule.cs` file, and add any additional dependencies in `RegisterServices`.
      1. Note that all sub-modules should have already declared their dependencies themselves, so you may have nothing to do here.

   1. Edit all the `appsettings.*.json` files, and remove any sections not needed by the modules you are hosting, or by the base configuration of the host.

4. The last thing to do is take care of inter-host communication, as detailed below.

#### Inter-Host Communication

The next thing you will need to do is identify inter-host communication that is required between the modules of your new Host project and the modules of the other Host projects. Both, communication **from your host project** and communication **to your host project**.

All communication between subdomains is done via ports and adapters. However, the adapters that you see in use in the `ApiHost` project will more than likely be taking advantage of the fact that they can run very fast (and more reliably) as "in-process" calls between the "Application Layers". This is a convenience.

For example, the adapter used to access the `EndUser` subdomain from other subdomains (in `ApiHost`) is the `EndUsersInProcessServiceClient`, which is an adapter that assumes that the `EndUserModule` is deployed in the same host as the other module that is using the port `IEndUsersService` (e.g., the `Identity` subdomain). Thus, this in-process adapter uses the `IEndUserApplication` directly.

This is all very convenient when both modules/subdomains are deployed in the same host.

However, suppose you are splitting up the modules into different deployable hosts. In that case, those subdomains need to communicate using different adapters, which will need to be HTTP service clients as opposed to in-process service clients.

For example, for subdomains (in another host) that need to communicate with the  `Cars` subdomain through the `ICarsService` port, you must provide (and inject) an HTTP adapter, like the `CarsHttpServiceClient`.

> Note: Don't forget to provide HTTP service client adapters in both directions, to and from your new host.
>
> Remember: Even though you may be communicating between several hosts now, as long as the hosts are deployed in the same data center as each other (assuming the same cloud provider), even though they are now using HTTP calls (instead of in-process calls), the speed of those calls should be very fast (perhaps sub ~100ms) as your hosts are likely to be hosted very close together (physically). Sometimes, the cloud provider keeps this latency optimized when components are physically close. But, reliability is still an issue.

In your HTTP service client adapter, when making a remote HTTP call, you must relay several HTTP headers containing important information since now you are stringing together several HTTP calls between the independently deployed hosts.

Two of those important details are:

1. The Authorization token (JWT access_token) that was presented (for the specific user) to the first API host called by a client. (i.e., `ICallerContext.Authorization`)
2. A correlation ID that identifies the original request to the first API host and can be used to correlate calls to other hosts to enhance diagnostics. (i.e., `ICallerContext.CallId`)

Other context can be added, but these two pieces are critical in order for different API hosts to collaborate effectively.

Your HTTP service client adapters should, therefore, use the `IServiceClient` port to communicate between different hosts, and you should inject the `InterHostServiceClient` adapter to make these calls.

> Note: The `InterHostServiceClient` will automatically forward the headers, as described above, and implement a retry policy (exponential backoff with jitter) to try and contact the other remote host.

#### Internal versus External Communication

Now, HTTP service clients are going to require "public" APIs to be created to support inter-host communication as described above. Many of these APIs do not exist in the codebase, since we've applied the YAGNI rule in NOT building them.

Furthermore, some of these API calls will not be intended to be used by any "untrusted" clients.

For example, we don't want anyone on the internet connecting to our API and making a direct call to an API that, say, returns the sensitive tenant-specific settings used for connecting to and querying a specific tenant's database. But we may need to have a "private" API that does give that data to a subdomain deployed on another host, that may need it.

These "private" API calls should not be publicly documented or advertised (i.e., in SwaggerUI) nor accessible to just any client on the internet, but they do need to be accessible to HTTP from trusted parties. They should be protected with some further mechanism that can only be used by other sanctioned/trusted hosts that we control.

In a "public" API call, we can make the call and include the necessary headers, and the host will treat this call like any other "public" API call, whether that call originated from a client directly or via another host (e.g., involved in a multi-step saga of some kind).

In a "private" API call, we still want to impersonate the original user that made the originating API call (that we are now relaying to another host), but we also need a further level of authentication to identify the sanctioned host forwarding the call as a "private" API call. As opposed to a client making this call directly.

> Note: Direct calls to "private" APIs cannot be allowed from clients directly, only from sanctioned and trusted hosts.

The mechanism that must be used to enforce this on "private" API calls can be implemented in infrastructure by an API gateway, VPN, or other common networking infrastructure. And/Or it can be enforced in the application using a mechanism like HMAC authentication or a similar mechanism that sends a shared secret in the request between the hosts that is validated by the destination host.

For "private" API requests, you declare them with the `isPrivate` option on the `RouteAttribute`.

For example,

```c# 
[Route("/organizations/{id}/settings", OperationMethod.Get, AccessType.Token, isPrivate = true)]
[Authorize(Roles.Platform_Operations)]
public class GetOrganizationSettingsRequest : TenantedRequest<GetOrganizationSettingsResponse>
{
    public required string Id { get; set; }
}
```

> Note: The above request is secured for `AccessType.Token` which means that a call to this API must include a Bearer token to identify the calling user. But, this endpoint is also marked as "private," so it must also include "private" API protection information as well.

#### Authentication & Authorization

As described above, when making inter-host communication, it is important to forward authentication information (like the original JWT access_token) between hosts, so that the host can identify the original calling user.

Beyond that, there is not much else to do.

All hosts will implement the same authorization mechanisms, which may, in some cases, require the host to communicate with other hosts.

#### Multi-tenancy

A final note of multi-tenancy. Implementation [details can be found here](0130-multitenancy.md).

In order to support multi-tenancy, each inbound request needs to identify the specific tenant it is destined for, with some kind of tenant ID.

This tenant ID can be determined in a number of ways, and the de facto mechanism is to include an `OrganizationId` in the request body or query string.

> Other options include using host headers, or custom domains.

In any case, resolving the given tenant ID to an existing bona fide organization, and then ensuring that the caller (if identified) has a membership to that organization is an important validation step in the request pipeline., for every inbound call.

Since this validation process is required on many of the endpoints of most "tenanted" subdomains, access to the `Organizations` and `EndUser` subdomains will be required to complete this process.

If those subdomains are hosted in other hosts (from the `Organizations` and `EndUser` subdomains), the API calls that need to be made may incur significant overhead overall. This can lead to [fan out](https://en.wikipedia.org/wiki/Fan-out_(software)) and performance degradation.

There are solutions to this problem, primarily in caching these kinds of checks, which may be required when splitting subdomains into different hosts, depending on which subdomains are split out.
