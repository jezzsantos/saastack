# Web Framework

## Design Drivers

1. We want to leverage standard-supported Microsoft ASP.NET web infrastructure (that is well known across the developer community), rather than learning another web framework (like ServiceStack.net - as brilliant as it is).
   - We are choosing ASP.NET Minimal API's over ASP.NET Controllers.
2. We want to deal with Request and Responses that are related and organized into one layer in the code. We favor the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern).
   - We are choosing to use MediatR to relate the requests and responses and endpoints into handlers
3. Minimal API examples (that you learn from) are simple to get started with, but difficult to organize and maintain in larger codebases especially when we are separating concerns into different layers.
   - We are seeking patterns that allow us to separate concerns and slit them into de-coupled layers

4. Web APIs are most often related to subdomains (and/or audiences) and typically grouped together for easier organization. We want a design that is easier to define and organize the API into pluggable modules.
   - We are choosing to encapsulate all web host configurations into one place for reuse across one or more web hosts.
   - We are choosing to implement a pluggable module pattern, (with host reusable host configuration) that makes moving and grouping multiple subdomains of APIs between web hosts easy
   - We are choosing to support a bespoke pattern for aggregating related APIs into a single class, to simplify declarative syntaxes. We are choosing to use source generators to convert this code into Mediatr handlers
5. We want simple cross-cutting concerns like validation, authentication, rate-limiting, etc. to be easily applied at the module level, or at individual endpoint level.
   - We are choosing to use FluentValidation + MediatR to wire-up and automatically validate all requests (where a validator is provided by the author)
6. We want all aspects of the web API to be testable.
   - We are choosing to use MediatR to support dependency injection into handlers
7. We want to support `async` to offer the option to optimize IO-heavy request workloads further down the stack.
   - All API declarations will be `async` by default
8. We are striving to establish simple-to-understand patterns for the API author while using essential 3rd party libraries, but at the same time, limit the number of dependencies on 3rd party libraries.

## Implementation

### Overview

We are establishing our own authoring patterns built on top of ASP.NET Minimal API, using MediatR handlers, that make it easier to declare and organize endpoints into groups within subdomains.

We are then leveraging FluentValidation for request validation.

We are integrating standard ASP.NET services like Authentication and Authorization.


### Modularity

One of the distinguishing design principles of a Modular Monolith over a Monolith is the ability to deploy any, all, or some of the subdomains/vertical slices (which includes its APIs) in any number of deployment units.

Taking this to the extreme of one subdomain/vertical slice per web host, you would end up with granular microservices. However, smaller steps towards that full microservices implementation are also very necessary to balance cost with complexity in distributed systems as they expand, depending on the stage of the SaaS product.

> We recommend starting with one deployment unit (a.k.a Monolith). Then, next, as load increases on the system, identify the "hot" subdomains and move them to their own web host, while grouping the remaining subdomains together into other hosts. Continue like this until you have a suitable balance of subdomains and hosts, that can be afforded.

The ability to deploy any (vertical slice/subdomain) of the code to a separate web host, should be quick and easy to accomplish, without expensive re-engineering. This is the primary value of starting with a modular monolith.

One of the essential things that has to be easy to do, is to group some endpoints (of a subdomain) with all the other components of the vertical slice and host it in any deployable unit.

> Communications between subdomains will already be decoupled via adapters and buses/queues.

This is how it is done.

1. Each WebApi project (one per vertical slice/subdomain) will define one or more API classes derived from `IWebApiService`. (See next section for details)
2. The WebApi project will reference a custom Source Generator to convert the `IWebApiService` into one or more MediatR handlers and Minimal API registrations.
   - Include the following XML in the `*.csproj` file of your WebApi project,
   ```xml
   <!-- Runs the source generator (in memory) on build -->
   <ItemGroup>
       <ProjectReference Include="..\Infrastructure.WebApi.Generators\Infrastructure.WebApi.Generators.csproj"
                              OutputItemType="Analyzer"
                              ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. These handlers and registrations will be automatically wired into the ASP.NET runtime in the web host `Program.cs`.
5. All dependencies of the WebApi project will be registered in the ASP.NET runtime automatically.

To make all this happen, a module class derived from `ISubDomainModule` needs to be created in each WebApi project.

In this class, you will need to declare the following things:

1. The assembly containing the API classes derived from `IWebApiService` is usually the same assembly where this module is defined.
2. Make a call to the `app.RegisterRoutes()` method on the Source Generated class called `MinimalApiRegistration`. Which also usually exists in the same assembly as the where this module is defined.
3. Register any dependencies that your WebApi project has for the endpoints, and dependencies for the remaining components in the layers of the subdomain/vertical slice.

Finally, this custom module class is then added to the list of other modules in `HostedModules` class, alongside the `Program.cs` of the web host project where this API is to be hosted.

### Endpoints

The design of Minimal APIs makes developing 10s or 100s of them in a single project quite unwieldy. And the examples being learned from do little to demonstrate how to separate concerns within them in more complex systems. Since they are registered as handlers, there is no concept of groups of APIs. Whereas many API endpoints are naturally grouped or categorized. This is certainly the case when exposing a whole vertical slice/subdomain.

We have designed a bespoke pattern and grouping mechanism for related endpoints, that results in Minimal APIs.

1. There is typically one WebApi project per vertical slice/subdomain. (However multiple projects are possible for supporting separating audiences).

   > For example, `CarsApi`

2. In the WebApi project, you will define one or more API classes derived from `IWebApiService`.
   > For example, `CarsApi.cs`
3. In that class, you will define one or more endpoints (service operations) as instance methods.
   > For example,
   > ```c#
   > public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
   > ```
4. You will define the request and response types in separate files in the project: `Infrastructure.WebAPi.Interfaces` in a subfolder for the subdomain.
   > For example, `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarRequest.cs` and `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarResponse.cs`
   > The request class derives from `IWebRequest<TResponse>` and the response class derives from `IWebResponse`
6. You decorate the service operation/endpoint method with a `[WebApiRoute]` and define the route template and operation type: `Get`, `Search`, `Post`, `Put`, `Patch,` or `Delete`.
   > For example:
   >
   > ```c#
   > [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
   > ```
   > Note: the default API guidelines recommend using `PutPatch` in place of `Put` and `Patch`.
7. You inject any dependencies into a custom constructor.
   > Note: The constructor with the most number of parameters will be used at runtime.
8. You would then create a request validator, for each endpoint/service operation.
   > For example, named `GetCarRequestValidator`.

Normally, the endpoint code will simply delegate the HTTP request down to the next layer, which is the Application layer. There is not much else to do unless the API endpoint deals with streams, files, or other kinds of HTTP requests.

> All the other cross-cutting concerns, like exception handling, logging, validation, authentication etc are taken care of elsewhere in the web framework.

This method will simply map the request object into simple primitives (or DTOs) data types, and feed them to the next layer as function parameters. It will be returned a resource (as a DTO) from the Application layer, and this function will simply include that in the HTTP response.

For example,

```c#
    [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
    public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(new CallerContext(), request.Id, cancellationToken);
        return Results.Ok(new GetCarResponse { Car = car });
    }
```

> Note: you cannot pass request objects to the next layer, as that would mean a dependency in the wrong direction.
>
> Note: In most cases, there is no need for any mapping code in this method (apart from simple object deconstruction to function parameters) This saves the author from defining another layer of mapping code.

Therefore, this layer is generally pretty simple, and, as such, does not usually warrant any unit testing. API Integration tests will fully cover and ensure that this layer is wired up correctly.

> With the exception of the cases where the method is doing more than just delegating a simple call to the application layer.

### Validation

We are using [FluentValidation](https://docs.fluentvalidation.net/) to validate all API requests.

FluentValidation validates the whole HTTP request as one document, and it is capable of providing detailed messages for one or more violations in the same request.

> Validation messages (`HTTP 400 - BadRequest`) messages usually contain full details about what is wrong with the request for each of the violations

As an author of an API endpoint, simply create a `AbstractValidator<TRequest>` class for each request, in your WebApi project, and it will be wired up automatically and will be executed automatically at runtime.

1. Create a validator class for each request in an endpoint.
   > For example, `public class GetCarRequestValidator : AbstractValidator<GetCarRequest>`
2. Create a default constructor, and add one or more `RuleFor` statements to verify that the data in the request is valid for this request.
   > For example,
   >
   >  ```c#
   >  RuleFor(req => req.Id)
   >  	.IsEntityId()
   >  	.WithMessage(Properties.Resources.GetCarRequestValidator_InvalidId);
   >  ```
   >
   > Note: You can inject dependencies as parameters to the constructor
3. Create a new resource string in the `Resources.resx` of the WebAPi project.
4. Write a unit test spec in the unit tests project of your WebApi project.

### Async

All API endpoints (service operations) will be declared as `async`, in the API layer.

While this may not be necessary, this is to support async operations in lower layers of the vertical slice/subdomain.

> Note: To support `async` properly anywhere within a single HTTP request, we need to `async` from the entry point down all the way to the response.

### Authentication and Authorization

TBD

### Exception Handling

TBD

### Logging

TBD

### Wire Formats

TBD

- what are the options, how to ask for others, what is the default?
- Dates and other data type formats for JSON, and how to change?
- Casing for JSON? and how to change

### Rate Limiting

TBD

### Swagger

TBD

# Credits

Many of the implementation patterns were inspired by content created by [Nick Chapsas](https://www.youtube.com/@nickchapsas)