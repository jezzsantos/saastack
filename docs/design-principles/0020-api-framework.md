# Web Framework

## Design Drivers

1. We want to leverage standard-supported Microsoft ASP.NET web infrastructure (that is well known across the developer community), rather than learning another web framework (like ServiceStack.net - as brilliant as it is).
   - We are choosing ASP.NET Minimal API's over ASP.NET Controllers.
2. We want to deal with Request and Responses that are related and organized into one layer in the code. We favor the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern).
   - We are choosing to use MediatR to relate the requests responses and the endpoints into handlers
3. Minimal API examples (that you learn from) are simple to get started with but difficult to organize and maintain in larger codebases, especially when we are separating concerns into different layers.
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

An example of a way we would prefer to define our endpoints related to a resource (e.g., a `car`), would look like a single class like this:

```c#
public class CarsApi : IWebApiService
{
    private readonly ICarsApplication _carsApplication;
    private readonly ICallerContext _context;

    public CarsApi(ICallerContext context, ICarsApplication carsApplication)
    {
        _context = context;
        _carsApplication = carsApplication;
    }

    [AllowAnonymous]
    [WebApiRoute("/cars", WebApiOperation.Search)]
    public async Task<IResult> Get(SearchCarsRequest request, CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllCarsAsync(_context, request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);
        return Results.Ok(new SearchCarsResponse { Cars = cars });
    }

    [AuthorizeForAnyRole(OrganisationRoles.Reserver, OrganisationRoles.Manager)]
    [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
    public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);
        return Results.Ok(new GetCarResponse { Car = car });
    }

    [AuthorizeForAnyRole(OrganisationRoles.Manager)]
    [WebApiRoute("/cars", WebApiOperation.Post)]
    public async Task<IResult> Post(RegisterCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.RegisterCarAsync(_context, request.Make, request.Model,
            request.Year, cancellationToken);
        return Results.Ok(new RegisterCarResponse { Car = car });
    }
    
    [AuthorizeForAnyRole(OrganisationRoles.Manager)]
    [WebApiRoute("/cars/{i}/offline", WebApiOperation.Post)]
    public async Task<IResult> PutPatch(TakeOfflineCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.TakeOfflineCarAsync(_context, request.Id, request.Reason, request.StartAtUtc, request.EndAtUtc, cancellationToken);
        return Results.Ok(new TakeOfflineCarResponse { Car = car });
    }
}
```

## Implementation

### Overview

We are establishing our own authoring patterns built on top of ASP.NET Minimal API, using MediatR handlers, that make it easier to declare and organize endpoints into groups within subdomains.

We are then leveraging FluentValidation for request validation.

We are integrating standard ASP.NET services like Authentication and Authorization.


### Modularity

One of the distinguishing design principles of a Modular Monolith (over a Monolith) is the ability to deploy any, all, or some of the subdomains/vertical slices (which includes its APIs) in any number of deployment units, at any time.

Taking this to the extreme of one endpoint/subdomain/vertical slice per deployed unit (per web host), you would end up with very granular microservices. However, in reality, for small teams, moving forward with larger deployments in smaller steps towards that full microservices implementation is very necessary to balance cost with complexity in distributed systems as they expand (according to the stage of the SaaS business).

> Recommendation: With a small team and limited budget, we recommend starting with one deployment unit (a.k.a a Monolith). Then, next, as load increases on the system, identify the "hot" subdomains/vertical slices and group them into their own web host while grouping the remaining subdomains together into other web hosts. Continue like this until you have a suitable balance of subdomains and hosts, that can be afforded.

The ability to deploy any (vertical slice/subdomain) of the code to a separate web host should be quick, easy, and safe to accomplish (between releases) without expensive re-engineering (some minimal engineering of HTTP adapters is required and expected). This is the primary value of starting with a modular monolith.

One of the essential things that has to be easy to do, is to group some endpoints (of a subdomain) with all the other components of the vertical slice and host it in any deployable unit.

> Communications between subdomains will already be decoupled via adapters and buses/queues.

This is how it is done.

#### Registering the Module

Once you have defined your endpoints (see next section), a module class derived from `ISubDomainModule` needs to be created in each WebApi project.

For example, in the project and folder: `CarsApi/CarsApiModule.cs`

- ```c#

public class CarsApiModule : ISubDomainModule
{
public Assembly ApiAssembly => typeof(Apis.Cars.CarsApi).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(Car), "car" }
    };

    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get { return (_, services) => { services.AddScoped<ICarsApplication, CarsApplication.CarsApplication>(); }; }
    }

}
  ```

In this class, you will need to declare the following things:

1. The assembly containing the API classes derived from `IWebApiService` is usually the same assembly where this module is defined.
2. Make a call to the `app.RegisterRoutes()` method on the Source Generated class called `MinimalApiRegistration`. Which also usually exists in the same assembly as the where this module is defined.
3. Register any other dependencies that your WebApi project has for the endpoints and dependencies for the remaining components in the layers of the subdomain/vertical slice.

Finally, the custom module is then added to the list of other modules in `HostedModules` class, alongside the `Program.cs` of the web host project, where this API is to be hosted.

For example, in the ApiHost project: `ApiHost1/HostedModules.cs`

```c#
public static class HostedModules
{
    public static SubDomainModules Get()
    {
        var modules = new SubDomainModules();
        modules.Register(new CarsApiModule());

        return modules;
    }
}
```

> Note: this method `HostedModules.Get()` will be called in the startup of the Host project.

### Endpoints

The design of Minimal APIs makes developing 10s or 100s of them in a single project quite unwieldy to manage well.

> All the examples out there being learned from do little to demonstrate how to separate concerns within them in more complex systems. Since they are registered as individual handlers, there are not good collective ways to declare groups of related APIs. Especially since most REST APIs are grouped around resources. This is certainly the case when exposing a whole vertical slice/subdomain in a module.

So, we have designed a coding pattern and grouping mechanism for related endpoints that results in automatic registration of Minimal APIs for you.

#### Creating the API class

1. There is typically one WebApi project per vertical slice/subdomain. For example, `CarsApi`

   > However, multiple projects are possible to support separating audiences

2. Each WebApi project (one per vertical slice/subdomain) will define one or more API classes derived from `IWebApiService`.

   - For example, in the project and folder: `CarsApi/Apis/Cars/CarsApi.cs`

   - ```c#
     public class CarsApi : IWebApiService
     {
          private readonly ICarsApplication _carsApplication;
          private readonly ICallerContext _context;

          public CarsApi(ICallerContext context, ICarsApplication carsApplication)
          {
              _context = context;
              _carsApplication = carsApplication;
          }
         
         [AuthorizeForAnyRole(OrganisationRoles.Reserver, OrganisationRoles.Manager)]
         [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
         public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
         {
             var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);
             return Results.Ok(new GetCarResponse { Car = car });
         }
         
         ...other methods
     }
     ```

3. You will define the request and response types in separate files in the project: `Infrastructure.WebApi.Interfaces` in a subfolder for the subdomain. For example,

   - `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarRequest.cs` and `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarResponse.cs`

   > Note: The request class derives from `IWebRequest<TResponse>`, and the response class derives from `IWebResponse`
   >
   > You define any incoming data fields as `public` properties on the request class, using either primitive types or other DTO resources. For example, `public string Make { get; set; }` or `public CarManufacturer Manufacturer { get; set; }`
   >
   > You define a single outgoing DTO field named the same as the resource. For example, `public Car Car { get; set; }`
   >
   > All these resource DTO types are defined in the project folder: `Application.Interfaces/Resources`

4. You decorate each service operation/endpoint method with a `[WebApiRoute]` and define the route template and operation type: `Get`, `Search`, `Post`, `PutPatch`, or `Delete`.

   - For example:

   ```c#
   [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
   ```

   > Note: Your route should always begin with a leading slash `/`, and you can substitute into the route any public property you define in your request class. For example, `/cars/{id}` where `{id}` refers to the property `public string Id { get; set; }`

5. You inject any dependencies into a custom constructor of yours.

   1. For example:
   ```c#
   public CarsApi(ICallerContext context, ICarsApplication carsApplication)
   {
       _context = context;
       _carsApplication = carsApplication;
   }
   ```
   > Note: Only the constructor that is `public` and the one with the most number of parameters will be used at runtime!

6. You would then add a validation class to validate the inbound request (see next section)

7. Lastly, you would write an integration test to test that your API works in the test category: `Integration.Web`.

   - For example, in the project and folder: `CarsApi.IntegrationTests/CarsApiSpec.cs`

   ```c#
   [Trait("Category", "Integration.Web")]
   public class CarsApiSpec : WebApiSpecSetup<Program>
   {
       public CarsApiSpec(WebApplicationFactory<Program> factory) : base(factory)
       {
       }
   
       [Fact]
       public async Task WhenGetCar_ThenReturnsCar()
       {
           var result = await Api.GetFromJsonAsync<GetCarResponse>("/cars/1234");
   
           result?.Car.Id.Should().Be("1234");
       }
   }
   ```

Normally, the code in each service operation will simply pass the HTTP request down to the next layer, which is the Application layer.

This method (service operation) will simply deconstruct the request object into simple primitive fields or DTOs, and feed them to the next layer as function parameters.

> Note: Usually, there is not much else to do unless the API endpoint deals with streams, files, or other kinds of HTTP requests.

> Note: Other cross-cutting concerns (e.g. exception handling, logging, validation, authentication, etc.) are taken care of elsewhere in the web framework.

From that Application layer, a resource (DTO) will be returned, and this function will simply map that result into the response.

> A typical service operation would look like this:

```c#
    [AuthorizeForAnyRole(OrganisationRoles.Reserver, OrganisationRoles.Manager)]
    [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
    public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);
        return Results.Ok(new GetCarResponse { Car = car });
    }
```

> Note: You must never pass request objects directly to the Application layer, as that would mean a dependency in the wrong direction. The Application layer should never be coupled to the API layer!
>
> Note: In almost all cases, there is no need for an additional mapping layer in this method, and instead, we use simple object deconstruction to function parameters to pass the request parameters to the application layer (which are almost always primitive types). This saves the author one more mapping layer of code to maintain.
>
> If you were to implement a mapping layer here, it would be the fifth and sixth mapping layers needed in total (end to end). So this is a convenient shortcut to avoid that extra work for most cases

#### Testing the API class

Since this layer is generally pretty simple (essentially just a delegate call), it does not usually warrant any unit testing. Unit testing this layer, for such simplistic methods, is unnecessary.

> Note: However, API integration tests are necessary, and they will fully cover this layer to ensure it is wired up correctly.

### Validation

We are using [FluentValidation](https://docs.fluentvalidation.net/) to validate all API requests.

FluentValidation validates the whole HTTP request as one document, and it is capable of providing detailed messages for one or more violations in the same request.

> Validation messages (`HTTP 400 - BadRequest`) messages usually contain full details about what is wrong with the request for each of the violations

#### Creating a Request Validator

As an author of an API endpoint, simply create a `AbstractValidator<TRequest>` class for each request, in your WebApi project, and it will be wired up automatically and will be executed automatically at runtime.

1. Create a request validator for each endpoint/service operation, in the same folder as the API class.

   - For example, in the project folder: `CarsApi/Apis/Cars/GetCarRequestValidator.cs`

   ````c#
   [UsedImplicitly]
   public class GetCarRequestValidator : AbstractValidator<GetCarRequest>
   {
       public GetCarRequestValidator(IIdentifierFactory idFactory)
       {
           RuleFor(req => req.Id)
               .IsEntityId(idFactory)
               .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
           
           RuleFor(req => req.Make)
               .Matches(Validations.Cars.Make)
               .WithMessage(Resources.GetCarRequestValidator_InvalidMake);
       }
   }
   ````

   > Note: you can inject any needed dependencies into the constructor of the validator. For example, `IIdentifierFactory` to validate `Id` fields of the request.
   >
   > Note: You would also create new resource strings, for each of the error messages of each field, in the `Resources.resx` file of the same API project.

2. Lastly, you will write some unit tests for the validator.

   - For example, the project folder: `CarsApi.UnitTests/Apis/Cars/GetCarsRequestValidatorSpec.cs`
    ```c#
    public class GetCarRequestValidatorSpec
    {
        private readonly GetCarRequest _req;
        private readonly GetCarRequestValidator _validator;
    
        public GetCarRequestValidatorSpec()
        {
            var idFactory = new Mock<IIdentifierFactory>();
            idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
                .Returns(true);
            _validator = new GetCarRequestValidator(idFactory.Object);
            _req = new GetCarRequest
            {
                Id = "anid",
                Make = "amake"
            };
        }
    
        [Fact]
        public void WhenAllProperties_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_req);
        }
        
        
        [Fact]
        public void WhenMakeIsNull_ThenFails()
        {
            _req.Make = null;
            
            _validator.ValidateAndThrow(_req);
        }
    }
    ```

Your validator will be wired up automatically and executed automatically at run time.

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

### Request Correlation

TBD

### Rate Limiting

TBD

### Swagger

TBD

# Credits

Many of the implementation patterns were inspired by content created by [Nick Chapsas](https://www.youtube.com/@nickchapsas)