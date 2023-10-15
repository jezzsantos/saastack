# Web API Framework

## Design Principles

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

## Preferred Declarative syntax

This is an example of the declarative way we prefer to define our endpoints, in a way that relates them to a specific resource (e.g., a `Car`):

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

    [AuthorizeForAnyRole(OrganisationRoles.Manager)]
    public async Task<ApiDeleteResult> Delete(DeleteCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.DeleteCarAsync(_context, request.Id, cancellationToken);
        return () => car.HandleApplicationResult();
    }

    [AuthorizeForAnyRole(OrganisationRoles.Reserver, OrganisationRoles.Manager)]
    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }

    [AuthorizeForAnyRole(OrganisationRoles.Manager)]
    public async Task<ApiPostResult<Car, GetCarResponse>> Register(RegisterCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.RegisterCarAsync(_context, request.Make, request.Model, request.Year,
            cancellationToken);

        return () => car.HandleApplicationResult<GetCarResponse, Car>(c =>
            new PostResult<GetCarResponse>(new GetCarResponse { Car = c }, $"/cars/{c.Id}"));
    }

    [AllowAnonymous]
    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAll(SearchAllCarsRequest request,
        CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllCarsAsync(_context, request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse { Cars = c.Results, Metadata = c.Metadata });
    }

    [AuthorizeForAnyRole(OrganisationRoles.Manager)]
    public async Task<ApiPutPatchResult<Car, GetCarResponse>> TakeOffline(TakeOfflineCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.TakeOfflineCarAsync(_context, request.Id!, request.Reason, request.StartAtUtc,
            request.EndAtUtc, cancellationToken);
        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }
}
```

where each service operation (method above) would have a unique request DTO that would be defined like this:

```c#
[Route("/cars/{id}", ServiceOperation.Get)]
public class GetCarRequest : IWebRequest<GetCarResponse>
{
    public required string Id { get; set; }
}
```

AND, we prefer NOT to have to create MediatR class like this, for every single one of those methods.

```c#
    public class GetCarRequestHandler : IRequestHandler<GetCarRequest, IResult>
    {
        private readonly ICallerContext _context;
        private readonly ICarsApplication _carsApplication;

        public GetCarRequestHandler(ICallerContext context, ICarsApplication carsApplication)
        {
            this._context = context;
            this._carsApplication = carsApplication;
        }

        public async Task<IResult> Handle(GetCarRequest request, CancellationToken cancellationToken)
        {
            ... the body of the method
        }
    }
```

AND have to register the minimal API's like this:

```c#
            carsGroup.MapGet("/cars/{id}",
                async (IMediator mediator, [AsParameters] GetCarRequest request) =>
                     await mediator.Send(request, CancellationToken.None))
                .AddEndpointFilter<FilterA>()
                .AddEndpointFilter<FilterB>();;
```

since all the code above, is:

1. Is very boiler-plate, tedious to type out for every endpoint, and can easily lead to typos
2. It repeats the same things in every handler class (like the constructor and fields)
3. There is no design-time binding between the minimal API route registration and the MediatR handler to make sure they are properly bound when things change
4. You need to maintain 2 pieces of code together when you make changes, otherwise the API just stops responding!

## Implementation

This is how the web framework comes together in SaaStack.

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

```c#
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

### Declaring APIs

* We are establishing our own authoring patterns built on top of ASP.NET Minimal API, using MediatR handlers, that make it easier to declare and organize endpoints into groups within subdomains.
* We are then leveraging FluentValidation for request validation.
* We are integrating standard ASP.NET services like Authentication and Authorization.
* We are adding additional `IEndpointFilter` (and MediatR `IPipelineBehavior`) to provide the request and responses we desire.

The design of Minimal APIs makes developing 10s or 100s of them in a single project quite unwieldy to manage well.

> All the examples out there (teaching minimal APIs) do little to demonstrate how to separate concerns across them in more complex systems. Since they are registered as individual handlers, there are not good collective ways to declare groups of related APIs. Especially since most REST APIs are grouped around resources. This is certainly the case when exposing a whole vertical slice/subdomain in a module.

A nicer way is to use a Source Generator to read the declarative code, and do the heavy lifting for us by generating the boiler plate code, reliably.

Then we use Roslyn analyzers (and other tooling) to guide the author in creating the correct declarative syntax.

#### Creating the API class

1. There is typically one WebApi project per vertical slice/subdomain. For example, `CarsApi`

   > However, multiple projects are possible to support separating audiences

2. Each WebApi project (one per vertical slice/subdomain) will define one or more API classes derived from `IWebApiService`.

   - For example, in the project and folder: `CarsApi/Apis/Cars/CarsApi.cs`

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
     
         [AuthorizeForAnyRole(OrganisationRoles.Reserver, OrganisationRoles.Manager)]
         public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
         {
             var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);

             return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
         }
         
         ... other methods
     }
   ```
   > Note: There are analyzers to guide you in how to write the methods (service operations) in your class, just start by writing a public method, and follow the warnings in the IDE.
   >
   > Note: You must use a unique request type (`IWebRequest`) for each service operation

3. You will define the request and response types in separate files in the project: `Infrastructure.WebApi.Interfaces` in a subfolder for the subdomain. For example,

   - In `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarRequest.cs`

   ```c#
     [Route("/cars/{id}", ServiceOperation.Get)]
     public class GetCarRequest : IWebRequest<GetCarResponse>
     {
         public required string Id { get; set; }
     }
   ```
   and, in `Infrastructure.WebApi.Interfaces/Operations/Cars/GetCarResponse.cs`:
   ```c#
     public class GetCarResponse : IWebResponse
     {
         public Car? Car { get; set; }
     }
   ```

   > Note: The request class derives from `IWebRequest<TResponse>`, and the response class derives from `IWebResponse`
   >
   > You define any incoming data fields as `public` properties on the request class, using either primitive types or other DTO resources. For example, `public string Make { get; set; }` or `public CarManufacturer Manufacturer { get; set; }`
   >
   > You define a single outgoing DTO field named the same as the resource. For example, `public Car Car { get; set; }`
   >
   > All these resource DTO types are defined in the project folder: `Application.Interfaces/Resources`

4. You decorate the request DTO class with a `[Route]` attribute and define the route template and operation type: `Get`, `Search`, `Post`, `PutPatch`, or `Delete`.

   - For example:

   ```c#
     [Route("/cars/{id}", ServiceOperation.Get)]
     public class GetCarRequest : IWebRequest<GetCarResponse>
     {
         public required string Id { get; set; }
     }
   ```

   > Note: Your route should always begin with a leading slash `/`, and you can substitute into the route any public property you define in your request class. For example, `/cars/{id}` where `{id}` refers to the property `public string Id { get; set; }`
   >
   > Note: All service operations must share the same primary route segment, corresponding to your resource (e.g. they all start with `/cars`. This also permits sub resources (e.g. `/cars/wheels`, but not different primary resources in the same class.

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
    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
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

While this may not be always necessary, this pattern is established in this layer to correctly support async operations in lower layers of the vertical slice/subdomain.

> Note: To support `async` properly anywhere within a single HTTP request, we need to `async` from the entry point [all the way down](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) (the stack) to the response.

### Authentication and Authorization

TBD

### Error Handling

> Note: We are supporting both: throwing exceptions, and returning error results in this stack.

Any unhandled exceptions that are thrown (in any code, in a layer) will be caught and sanitized by an Exception Shielding strategy.

In this strategy:

* Exceptions are converted to [RFC7808](https://datatracker.ietf.org/doc/html/rfc7807) errors on the wire
* The HTTP StatusCode will always be `500 - Internal Server Error`
* An exception stack trace will only be included in the response in `TESTINGONLY` build configurations

We are also using the result type `Result<TValue, Error>` to pass errors through the Application and Domain layers up to the API layer, where that Error will have a `Code` that will be converted to an appropriate `HttpStatusCode` (with or without a message).

For `Result<TValue, Error>` that reach the API layer and that contain an error, they will be automatically converted into HTTP Status codes.

> There is a well-known mapping between the `Error.Code` (that has semantic meaning to the Application and Domain layers) to a `HttpErrorCode` (that has meaning to HTTP APIs).

### HTTP Status codes

By default, we set the status code of all responses based on the HTTP method used.

* If you `GET` a request, you will get a `200 - OK` response.
* If you `POST` a request, you will either get a `201 - Created` with the `Location` of the created resource in the response headers, or you will get a `200 - OK` response.
* If you `PUT` or `PATCH` a request, you will get a `202 - Accepted` response.
* If you `DELETE` a request, you will get a `204 - No Content` response

### Wire Formats

We support at least two wire formats:

* JSON
* XML

JSON is the default wire format for all responses, if none of the following are specified in a request, and the response contains any content (except binary).

You can request either JSON or XML responses in the following ways:

1. Including an `Accept` header, to be either: `application/json` or `text/xml`.
2. Including a `Content-Type` header with `application/x-www-form-urlencoded`, and a form field called `format`, to be either: `json` or `xml`.
3. Including a `Content-Type` header with `application/json`, and a JSON field called `format`, to be either: `json` or `xml`.
4. Including a `QueryString` called `format`, to be either: `json` or `xml`.

#### JSON

JSON requests:

* Fields in the body will be accepted in either camel-case or pascal-case.
* Dates and times will be accepted as either ISO8601 (strings) or as UNIX timestamps (numbers, in seconds)
* Enumerations will be only accepted as string values  (case-insensitive)

JSON responses:

- Fields in the response body will be camel-cased
- `null` values will not be written to the JSON response
- JSON will not include line breaks.
- `DateTime` values will be serialized in ISO8601 format
- `Enums` will output as string values (camel-case)

For example, a typical response might look like this:

```json
{
   "car": {
      "id": "car2",
      "bodyColor": "lightBlue",
      "createdAtUtc": "2023-09-24T23:43:21.6178588Z"
   }
}
```

#### XML

XML requests:

* XML is not accepted in requests

XML responses

- Fields in the response body will be pascal-cased
- `null` values will not be written to the JSON response
- XML will not include line breaks.
- `DateTime` values will be serialized in ISO8601 format
- `Enums` will output as string values (pascal-case)

### HTTP Clients

We have defined a `JsonClient` that can be used to call APIs, that provides convenient wrapper over `HttpClient`

It is typed to requests `IWebRequest<TResponse`, making it easier to access responses

### Request Correlation

We are correlating all requests coming through all the Hosts of our API.

> Whether we have a single host (monolith) or chain multiple hosts together (microservices).

On all inbound HTTP requests (of all hosts) we are looking for a correlation ID in any of these request headers:

* `Request-ID`
* `X-Request-ID`
* `Correlation-ID`
* `X-Correlation-ID`

If any of these headers are found in a request, we are then extracting that value and using it as the `ICallerContext.CallId` that is then passed down the call stack to all layers.
Otherwise we are fabricating a brand new value for the Correlation ID, and starting with that value.

> Correlation ID that are fabricated are simply UUIDs (see `Caller.GenerateCallId()`)

This correlation ID is appended as a `Request-ID` HTTP response header on all outbound responses.

Furthermore, when chaining together requests between modules, either in-process calls (via `ICallerContext`), or over HTTP service clients (using any of the `IHttpClient` clients), this value will be preserved for the entire chain of calls.

### Logging

Logging (and crash reporting) is performed through the `IRecorder` interface.

The `IRecorder` uses the ASP.NET `ILoggerFactory` under the covers to do all of its diagnostic logging.

It takes advantage of the configured loggers (and infrastructure already in ASP.NET).

For more details on the `IRecorder` see [Recording](0030-recording.md)

### Rate Limiting

TBD

### Swagger

TBD

### Credits

* The implementation of the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern) used here was heavily influenced by the REPR design in [ServiceStack](http://www.servicestack.net), due its declarative and explicit nature, the benefits of typed clients, and its testability aspects.
* Some of the implementation patterns (based on MediatR) were inspired by content created by [Nick Chapsas](https://www.youtube.com/@nickchapsas)