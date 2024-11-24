# Add an API Endpoint

## Why?

You want to expose your subdomain with a new API endpoint.

> We will refer to REST here because we intend to design and build REST endpoint (maturity level 4). That is, REST sans HATEOS.
>
> REST endpoints model workflows and processes, they are not intended to model database tables and CRUD. These design concepts should be avoided.

## What is the mechanism?

We are hosting our subdomains in web projects, and we deploy them in one or more hosts in the cloud (either on a web server like an "App Service" in Azure, or serverless like a "Functions Host" in Azure or a Lambda" in AWS).

In all cases, we are using ASPNET to provide the necessary pipeline to handle HTTP requests and serve HTTP responses.

We are using minimal APIs to define our endpoints (ultimately, as opposed to Controllers), but we are also providing a simpler structure to define and group those endpoints in your chosen subdomain.

> We actually use a source generator to do most of the heavy lifting of writing the code that produces the minimal API definitions, given a strongly typed declarative syntax of our own. Which makes your job much easier and less error-prone.

Furthermore, we are designing using the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern).

### The general approach

The general approach to building a new API is to start "outside-in", that is start from the design of the API and work inwards from there to the implementation details.

> Building a new API requires all the layers of the subdomain in place before you start (or create them as you go). You will need a project for each layer (Infrastructure, Application and Domain), and projects containing all the relevant tests.

We strongly recommend that before you start, you have some idea of the contract of an API to build and that it is not based upon a CRUD data-model, and it considers the entire lifecycle of a REST "resource".

A simple pseudo-description like:

```http
POST /myresources
{
	"AField": string, required,
	"AnotherField" : number, optional
	...etc
}
```

Please first read [REST API design and escaping CRUD](https://www.thoughtworks.com/insights/blog/rest-api-design-resource-modeling), from Thoughtworks.com, to move your thinking away from CRUD and towards modeling whole processes in REST.

Lastly, we strongly recommend writing your integration tests long before you implement any functionality in your API.

See [Integration testing](#integration-testing) below for more details. So many good things come from defining the request and responses before you build any code. (particularly, far better usability of the design of your API).

> You don't need to go too far with this, just get an initial design down, and ask yourself how you can make it easier to consume this API if you were the developer at the other end trying to use this API.

## Where to start?

You build an API endpoint in the `Infrastructure` project of your subdomain.

For example, in the `CarsInfrastructure` project

### Project structure

We first create a folder called `Api`.

> Any REST API can deal with one or more related REST "resources"; they are usually grouped, and can involve a root resource or child resources. We group those resources around common routes that use the same resource prefixes.

So, for each grouping, we create another sub-folder for that resource.

For example, a `Cars` sub folder.

Thus, we end up with a folder structure looking like this (for a single resource):

```
├── CarsInfrastructure.csproj
└── Api
    └── Cars
        └── CarsApi.cs
```

And, a folder structure looks like this (if we would have many closely related resources):

```
├── CarsInfrastructure.csproj
└── Api
    ├── Cars
    │   └── CarsApi.cs
    └── Trailers
        └── TrailersApi.cs
```

### API definition

The class file called `CarsApi.cs` would contain a `sealed` class that derives from `IWebApiService`

This class will define a collection of "service operations" for the same REST resource (i.e., `/cars`).

The class would have a constructor that commonly injects two dependencies:

1. `ICallerContextFactory` - this factory creates a new instance of an
   `ICallerContext` containing information about the user making the call, which we will need downstream in other code.
2. `ICarsApplication` this is the registered application layer class (i.e.,
   `CarsApplication`) that we will delegate the API request to, to process our API request.

```c#
public sealed class CarsApi : IWebApiService
{
    private readonly ICarsApplication _carsApplication;
    private readonly ICallerContextFactory _callerFactory;

    public CarsApi(ICallerContextFactory callerFactory, ICarsApplication carsApplication)
    {
        _callerFactory = callerFactory;
        _carsApplication = carsApplication;
    }
    
    ... service operations
}
```

### Service Operations

Then, for each REST endpoint that we want to support, we will define a "service operation" which is realized as a method in this class.

A "Service Operation" is essentially the same thing as an "endpoint", it is generally composed of:

* An HTTP request (likely in JSON)
* A route (that defines the collection of the REST resource)
* Some authorization declaration (most authenticated requests will limit access to some degree, either by role or by subscription level a.k.a. features)
* A response, which may or may not have a body (in JSON/XML/etc, if it does)

In SaaStack, we like to define the request and response objects independently and then couple them together since it provides advantages later for processing requests. Particularly with typed API clients.

> Reusing these request and response types is not an important consideration in this kind of design. These types are intended to be very specific (and very precise) to the use case we are modeling.
>
> However, if they contain types, then those types can be reused more generally, where it makes sense.

This is an example service operation:

```c#
public sealed class CarsApi : IWebApiService
{
    .. constructor and fields

    // This is an example service operation
    public async Task<ApiPostResult<Car, RegisterCarResponse>> RegisterCar(RegisterCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.RegisterCarAsync(_callerFactory.Create(), request.OrganizationId!,
            request.Year, request.Make, request.Model, cancellationToken);

        return () => car.HandleApplicationResult<Car, RegisterCarResponse>(c =>
            new PostResult<RegisterCarResponse>(new RegisterCarResponse { Car = c }));
    }

    ... other service operations
}
```

> This syntax is very specific to this codebase since it is processed for you by a source generator, into minimal APIs.

There is one C# method of this class for each endpoint you want to expose.

Essentially, the C# method always delegates the call to an appropriate application layer method.

For example, in this case, that's shown as `ICarsApplication.RegisterCarAsync(...)`

Then, this C# method calls
`HandleApplicationResult()` and, in the process, constructs a new instance of a response type, which will make up the HTTP response body. Since we are using
`Result<>` responses, the
`HandleApplicationResult()` method will detect if the result is an error and convert the response to the appropriate HTTP Status code (and content) for you automatically.

> This means that all delegated application layer calls must return a `Result<TResource, Error>` (or `Result<Error>`).

In Rider, there are several code templates ("Live Templates") that can scaffold these service operations for you:

* `postapi`, `getapi`, `putpatchapi` `searchapi` and `deleteapi`
* You simply type those words, and hit ENTER, and then fill out the template

```c#
public async Task<ApiPostResult<Resource, ActionResourceResponse>> ActionResource(ActionResourceRequest request, CancellationToken cancellationToken)
    {
        
        var resource = await _resourceApplication.ActionResourceAsync(_callerFactory.Create(), request.Id, cancellationToken);

        return () =>
            resource.HandleApplicationResult<Resource, ActionResourceResponse>(x => new PostResult<ActionResourceResponse>(new ActionResourceResponse { Resource = x }));
    }
```

Let's look at the rest of the signature now:

#### Return value

The return type of all these service operations is an `ApiResult`. There are several common choices of `ApiResult`:

* `ApiPostResult` - used to define a service operation for a POST request
* `ApiPutPatchResult` -used to define a service operation for a PUT or PATCH request
* `ApiDeleteResult` - used to define a service operation for a DELETE request
* `ApiGetResult` - used to define a service operation for a GET request (single resource)
* `ApiSearchResult` - used to define a service operation for a GET request (a collection of resources)
*
`ApiStreamResult` -  (rare) used to define a service operation for a GET request that returns binary data (i.e., images)

> There are several other flavors of `ApiResult`, that are used in specific cases:
>
>
`ApiEmptyResult` - can be used instead of the other kinds to denote an API that does not return any data in the result. (i.e., an
`HTTP 204 - No Content`)
>
> `ApiResult` - can be used in place of the others for PUT, PATCH, DELETE or GET methods, but not POST.

In most cases, these `ApiResult` return values include the type of the resource (i.e.,
`ApiResult<TResources, TResponse>` that is being returned in a successful call - or an `Error`).

#### Request and response types

Every service operation has a specific request and specific response type.

These types are ALWAYS defined in the `Infrastructure.Web.Api.Operations.Shared` project.

You might need to create a sub-folder in that project corresponding to your subdomain.

For example,

```
├── Infrastructure.Web.Api.Operations.Shared.csproj
└── Cars
    └── RegisterCarRequest.cs
    └── RegisterCarResponse.cs
```

To create a new request type, in Rider, right-click on the subfolder, and use the
`Add -> SaaStack -> API POST Request DTO` command, and fill out the template. Or you can hand-craft your own request types by copying from one of the other existing definitions in the codebase.

> If you decide to hand-craft your own definition, there will be code analysis rules to help you complete the correct structure.

For example,

```c#
/// <summary>
///     Registers a new car
/// </summary>
[Route("/cars", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class RegisterCarRequest : TenantedRequest<RegisterCarRequest, GetCarResponse>
{
    [Required] public string? Jurisdiction { get; set; }

    [Required] public string? Make { get; set; }

    [Required] public string? Model { get; set; }

    [Required] public string? NumberPlate { get; set; }

    [Required] public int Year { get; set; }
}
```

Let's take a look at this structure in detail.

##### Request type

First of all, this is always a `public class` and must have a public parameter-less constructor (for serialization)

It is always derived from an underlying `IWebRequest` or `IWebRequest<TResponse>` and typically references a separate
`TResponse` type that must derive from `IWebResponse`.

> There are several common types that make defining these request objects easier, such as
`TenantedRequest<TResponse>` and `UnTeantedRequest<TResponse>`.

If your request is returning any content in the HTTP response, you must define a
`TResponse` class, and it must live in a separate file next to the request type.

There are slightly different flavors of the
`TenantedRequest<TResponse>` return type, depending on what HTTP method you are using.

You would use
`TenantedSearchRequest<TResponse>` for SEARCH operations (since they require the processing of filters, sorts, and delivering metadata in responses for searching resource collections).

For example, to search for all cars, you would define this request:

```c#
public class SearchAllCarsRequest : TenantedSearchRequest<SearchAllCarsRequest, SearchAllCarsResponse>;
```

You would use a `TenantedDeleteRequest<TResponse>` for DELETE operations.

For example, to delete a car, you would define this request:

```c#
public class DeleteCarRequest : TenantedDeleteRequest<DeleteCarRequest>
{
    [Required] public string? Id { get; set; }
}
```

> Notice that in this case, the is no response type (`TResponse`), as this response deliberately returns no content.

Otherwise, you would use the standard `TenantedRequest<TResponse>` for all other POST, PUT, PATCH, and GET methods.

For example,

```c#
public class GetCarRequest : TenantedRequest<GetCarRequest, GetCarResponse>
{
    [Required] public string? Id { get; set; }
}
```

##### Fields

Your request type might include zero, one or more fields of data, that may be optional or mandatory for the client to provide your API endpoint.

Whether you are providing a field for a POST body, OR providing a field to represent a parameter in the query string, in both cases, you define a getter/setter property in the class.

There are some strict rules about how to declare these fields, that may not be obvious at the start.

1. They must be defined using C# primitive types or defined with fully serializable types defined in the
   `Application.Resources.Shared` project.

2. They must have public getters and setters (for serialization)

3. They cannot be declared using the
   `required` keyword in C#; otherwise, you run the risk that the runtime will throw serialization errors if a client fails to provide data in that specific field in the JSON request.

   > Warning: This is particularly pernicious because your API will throw a `500 - Internal Server Error` instead of a
   `400 - BadRequest` in this case (which is a bug you have to fix).

4. They cannot use the `Optional<T>` type, as those are not inherently serializable.

5. For reference types, they must be nullable (i.e., `public string? AName { get; set; }`) or have an initializer (i.e.,
   `public string? AName { get; set; } = "";`).

6. If you want them to appear in the OpenAPI specification as logically "required", then you need to add the
   `[Required]` attribute to each one.

Because of these C#/ASP.NET constraints, we recommend either declaring nullable reference types (i.e.,
`string?`) or use value type primitives (i.e. `int`, `DateTime` etc.).

You can also use simple collections like `List<string>()` or `List<TDto>()`, or even
`Dictionary<string,TDto>()`, and we recommend that you declare these with initializers as empty collections for ease of use downstream in the code (i.e.
`public List<string> ACollection { get; set; } = new List<string>();`)

> The use of other
`System.DataAnnotation` attributes may/may not add more detail to the OpenAPI specification and are completely optional.

##### Documentation

SaaStack automatically generates OpenAPI information for all your REST endpoints.

To make this easier for you to present a meaningful description for each operation, SaaStack enforces that you add a
`<summary>` element in the XML documentation for your request class.

This `<summary>` is then used in the OpenAPI documentation for your all your API consumers to see.

Furthermore, SaaStack automatically generates the possible error responses that your API will be producing, based on a set of default behaviors for different HTTP methods. Those errors are written into the OpenAPI documentation for you., but you can also add more or override existing ones by using the
`<response code="">` element in the documentation of the class.

For example,

```c#
/// <summary>
///     Authenticates a user with a username and password
/// </summary>
/// <response code="401">The user's username or password is invalid</response>
/// <response code="405">The user has not yet verified their registration</response>
/// <response code="423">The user's account is suspended or locked, and cannot be authenticated or used</response>
[Route("/passwords/auth", OperationMethod.Post)]
public class AuthenticatePasswordRequest : UnTenantedRequest<AuthenticatePasswordRequest, AuthenticateResponse>
{
    [Required] public string? Password { get; set; }

    [Required] public string? Username { get; set; }
}
```

> This is useful in specific cases when your API is throwing specific HTTP errors for specific use cases, to help clients act is different ways.

##### Route and method

Every REST endpoint (and service operation) has a unique URL route.

> See the [API Design Guidelines](../design-principles/0010-rest-api.md) for more details on how to define them.

You add the
`[Route]` attribute to each and every request type to define a route template, and define HTTP operation that you want to support.

For example,

```c#
[Route("/cars", OperationMethod.Post, AccessType.Token)]
public class RegisterCarRequest : TenantedRequest<RegisterCarRequest, GetCarResponse>
{
    ... fields of the request
}
```

In this case, we see the route for this REST resource collection `/cars`

The routes always need to start with a leading slash
`/` character and all request types for this subdomain, will have the same route prefix.

This value is actually a "template" that is used to pre-populated the fields in the request object, either from the URL path or from the query string.

For example, to populate the `Id` field of the request, you would define the route and request class like this:

```c#
[Route("/cars/{Id}", OperationMethod.Get, AccessType.Token)]
public class GetCarRequest : TenantedRequest<GetCarRequest, GetCarResponse>
{
    [Required] public string? Id { get; set; }
}
```

> At runtime, the `Id` of the car is extracted from the path of the route, and used to populate the `Id` property in the
`GetCarRequest` class instance.

Next, we define the HTTP method (`OperationMethod`) of the request.

We have simplified the choices of methods you can choose from:

* `Post` - for HTTP POST requests
*
`PutPatch` - for either PUT or PATCH methods (see the [API Design Guidelines](../design-principles/0010-rest-api.md) for why we don't currently support both)
* `Delete` - for HTTP DELETE requests
* `Search` - for HTTP GET requests, where you are returning a collection of a resource.
* `Get` - for HTTP GET requests, where you are returning a single resource

##### Authorization

If your endpoint is intended to only allow authenticated users to have access to it, and if you want to ensure that those users have the correct roles and billing subscription levels, then you can enforce those coarse grained authorization checks at the API level (as well as in the domain layer).

To make this take effect, you need to specify the 3rd parameter of the `[Route]` attribute to be something other than
`Anonymous`, to indicate the endpoint can only be called by an authenticated caller.

* `Token` - means the calling user is identified by a token in the request. i.e., a JWT bearer token in the
  `Autheorixation` header of the request.
*

`HMAC` - means that the request will use HMAC authentication to identify the calling user. This is only used for private API calls, not intended for the public to use.
* `Anonymous` - means no authentication mechanism is required. (even is a token is included in the request)

For example,

```c#
[Route("/cars", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class RegisterCarRequest : TenantedRequest<RegisterCarRequest, GetCarResponse>
{
    ... fields of the request
}
```

When you do this, you now have the option of adding a `[Authorize]` attribute to the class.

> By default, if you do not specify an `[Authorize]` attribute (when you specify a
`[Route]` attribute), the API call is always going to be asserting that the authenticated user has the following  roles:
`Platform_Standard` and features: `Platform_Basic`.

This is where you need to make sure that you have correctly defined whether your request is "Tenanted" or "Un-Tenanted".

> See the documentation on [Multitenancy](../design-principles/0130-multitenancy.md) and [Authorization](../design-principles/0090-authentication-authorization.md) particularly around roles and features.

For Tenanted requests, you can specify specific tenant based roles and features.

##### Response type

The response type is where you define the body of the response.

By convention, most requests result in returning a body, except for some DELETE requests.

> However, there are some common exceptions to this rule. For example, when you logically "cancel" a booking for a car you might decide to use the DELETE method for this operation, AND you might also decide to return a representation of the booking that is now canceled.
>
> Strictly speaking, this operation could/should be defined using the PUTPATCH  method instead, as it actually changes the state of the booking, instead of deleting it from existence.
>
> When deleting from existence, there is likely to be no content in the response

Most responses will be defined as deriving from the `IWebResponse` interface, which is simply a marker interface.

An example response type would be:

```c#
public class GetCarResponse : IWebResponse
{
    public Car? Car { get; set; }
}
```

However, there is one specific response type that you want to use for your SEARCH operations:

```c#
public class SearchAllCarsResponse : SearchResponse
{
    public List<Car>? Cars { get; set; }
}
```

This response type (`SearchResponse`) will render not only the collection of
`Cars` above, but it will also include another node that summarizes the number of results returned.

For example, defining this operation, notice that the result is populating not only the `Cars` collection, but also the
`Metadata` collection.

```c#
    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAll(SearchAllCarsRequest request,
        CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllCarsAsync(_callerFactory.Create(), request.OrganizationId!,
            request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse 
                                         { 
                                             Cars = c.Results, 
                                             Metadata = c.Metadata 
                                         });
    }
```

The JSON returned from this kind of response would look something like this:

```json
{
  "cars": [
    {
      "managers": [],
      "manufacturer": {
        "make": "Honda",
        "model": "Civic",
        "year": 2017
      },
      "owner": {
        "id": "user_iDWJEBu1kO0qQ62pILRjw"
      },
      "plate": {
        "jurisdiction": "New Zealand",
        "number": "ABC123"
      },
      "status": "Registered",
      "id": "car_foW7mGmbCUyGPpX1FgzVIw"
    }
  ],
  "metadata": {
    "filter": {
      "fields": []
    },
    "limit": 100,
    "offset": -1,
    "total": 1
  }
}
```

### Request Validator

Most all HTTP requests contain data that should be validated long before any code downstream is executed. This means that we are rejecting bogus requests before they consume any further resources.

> We use the [FluentValidation](https://docs.fluentvalidation.net) library to perform the inbound HTTP request validation, since it supports validation of the entire request in one go, and it gives excellent detailed errors that make debugging far easier for consuming API developers.

All requests are validated automatically if and only if an appropriate validation class is found in the codebase.

For each of your request types (service operations), there should ALWAYS be an associated validation type.

In the sub-folder where you defined your API class, you define a validation class using the name of the request type.

For example, for this request type:

```c#
public class RegisterCarRequest : TenantedRequest<RegisterCarRequest, GetCarResponse>
{
    [Required] public string? Jurisdiction { get; set; }

    [Required] public string? Make { get; set; }

    [Required] public string? Model { get; set; }

    [Required] public string? NumberPlate { get; set; }

    [Required] public int Year { get; set; }
}
```

We define this class for its validator:

```c#
public class RegisterCarRequestValidator : AbstractValidator<RegisterCarRequest>
{
    public RegisterCarRequestValidator()
    {
        RuleFor(req => req.Make)
            .Matches(Validations.Car.Make)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidMake);
        RuleFor(req => req.Model)
            .Matches(Validations.Car.Model)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidModel);
        RuleFor(req => req.Year)
            .InclusiveBetween(Validations.Car.Year.Min, Validations.Car.Year.Max)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidYear);
        RuleFor(req => req.Jurisdiction)
            .Matches(Validations.Car.Jurisdiction)
            .Must(req => Jurisdiction.AllowedCountries.Contains(req))
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidJurisdiction);
        RuleFor(req => req.NumberPlate)
            .Matches(Validations.Car.NumberPlate)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidNumberPlate);
    }
}
```

Some key things here:

1. The class name is identical to the request type, with the suffix `Validator`.
2. The class derives from `AbstractValidator<TRequest>`
3. The rules in this class are defined in the constructor, and nowhere else.
4. Most rules provide custom error messages, that are stored in the
   `Resources.resx` file of the same project where this class is defined. They are resources for easy reference in testing.
5. Most rules consume `Validation` expressions that are defined in the Domain project for the same subdomain.

Most of the "validators" you will use to construct each
`RuleFor` statement, are defined in [FluentValidation](https://docs.fluentvalidation.net) library.

However, there are a small number of "validators" that have been built specifically for this codebase to make your lives easier.

For example, validating resource identifiers, email addresses, and other common types have their own rules:

```c#    
public class AnExampleRequestValidator : AbstractValidator<AnExampleRequest>
{
    public AnExampleRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.EmailAddress)
            .IsEmailAddress()
            .WithMessage(Resources.AnExampleRequest_InvalidEmailAddress);
    }
}
```

In this case, we need to validate the inbound
`Id` property, and make sure it is formatted like all the other identifiers used for resource in the codebase. So, we dependency-inject the
`IIdentiferFactory` into the constructor, and we use the `IsEntityId(idFactory)` validator.

#### Custom Validation Expressions

Many of your rules will use validation expressions that you will need to define specifically for your subdomain. They can then be reused in the Infrastructure Layer (as we are doing here to validate the API calls) and then reused inside the Domain Layer in the rules of your aggregates and value objects.

In the Domain project for your subdomain, add a `public static class`class
`Validations`, and define all the validation expressions that are very specific to your subdomain.

For example,

```c#
public static class Validations
{
    public static class Car
    {
        public static readonly Validation Jurisdiction = new(@"^[\d\w\-\. ]{1,50}$", 1, 50);
        public static readonly Validation Make = CommonValidations.DescriptiveName(2, 50);
        public static readonly TimeSpan MinScheduledMaintenanceLeadTime = TimeSpan.FromHours(24);
        public static readonly Validation Model = CommonValidations.DescriptiveName(2, 50);
        public static readonly Validation NumberPlate = new(@"^[\d\w ]{1,15}$", 1, 15);
        public static readonly Validation Reason = CommonValidations.FreeformText(0, 200);

        public static class Year
        {
            public const int Min = 1900;
            public static readonly int Max = DateTime.UtcNow.Year + 1;
        }
    }

    public static class Unavailability
    {
        public static readonly Validation Reference = CommonValidations.DescriptiveName(1, 250);
    }
}
```

> Notice, that these definitions make extensive use of definitions elsewhere in the code base, such as in the
`CommonValidations` class (in `Domain.Interfaces` assembly).

#### Custom validators

Sometimes, it is necessary to build your own validators for common types that you find you are using in many requests.

For example, your request contains a collection of complex data objects, whereas normally you are dealing with a single property value.

```c#
public class CreateThingRequest : TenantedRequest<CreateThingRequest, CreateThingResponse>
{
    public List<AComplexThing> Things { get; set; }
}
```

where `AComplexThing` is defined as:

```c#
public class AComplexThing
{
    [Required] public string? AString { get; set; }
    
    public int ANumber { get; set; }
    
    ... other properties
}
```

Depending on the API you are designing, there may be some further complex rules about what these fields values can be, even relative to each other. For example, you might have to validate that
`ANumber` is greater than 1, if there is any value for `AString`.

In cases like this, you will want to define your own validator, that you can reuse in this request, and others that may deal with the same
`AComplexThing`.

For example, first you would define the validator for the request class:

```c# 
public class CreateThingRequestValidator : AbstractValidator<CreateThingRequest>
{
    public CreateThingRequestValidator()
    {
        RuleFor(req => req.Things)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CreateThingRequestValidator_InvalidThings);
        RuleForEach(req => req.Things)
            .SetValidator(new ComplexThingValidator());
    }
}
```

Then you would implement a new validator for each of the `ComplexThing` instances.

```c#
public class ComplexThingValidator : AbstractValidator<ComplexThing>
{
    public ComplexThingValidator()
    {
        RuleFor(req => req.AString)
            .NotEmpty()
            .WithMessage(Resources.ComplexThingValidator_InvalidAString);

        RuleFor(req => req.ANumber)
            .GreaterThan(1)
            .When(req => req.AString.HasValue())
            .WithMessage(Resources.ComplexThingValidator_InvalidANumber);

        ... other custom rules
    }
}
```

### Tests

As already discussed in [Create a Subdomain Module](010-subdomain-module.md) we are using the "outside-in" approach to implementing each of the APIs in a subdomain.

For each code element that we now build, you COULD write unit tests for them, specifically:

1. The API class (i.e., `CarsApi.cs`) and each service operation method in it.
2. Each of the Request and Response types for each of the service operations of your API (i.e., `RegisterCarRequest`,
   `RegisterCarResponse`).
3. Each of the validators for each of these request types (i.e., `RegisterCarRequestValidator`)

That is quite a bit of new code introduced into the subdomain, and it all certainly needs testing to some degree.

The only question is, what tests should we write for which components, and how far do we take that?

The truth is that you could write unit tests for each and very class we just built, but most of those tests are going to be pretty shallow, not testing much at all.

We recommend the following approach.

#### API class

The API class (i.e., `CarsApi.cs`).

We recommend that you don't bother unit testing this class unless you have written any code in any service operation beyond the simple: request deconstruction -> application method delegation -> and response mapping code, which is extremely common in all service operations - diminishing returns unit testing this.

Instead, just follow the same patterns as every other service operation in the code before you.

The forthcoming integration tests will cover it and ensure it is all wired up correctly and mapped properly.

> One exception to this rule, is when you are processing something that you want to extract from the HTTP request that is not normally supported. Such as specific headers or multipart bodies. In these cases, you will be introducing some new code to handle those cases, and we recommend that you do unit test this code, if there is significant processing involved in it.
>
> For example, an example of handling a binary `multipart-form` body can be seen in the
`ImagesApi.UploadImage` service operation.

#### Request and response types

Each of the Request, and Response types for each of the service operations of your API (i.e., `RegisterCarRequest`,
`RegisterCarResponse`).

These types are Data Transfer Objects (DTOs) and are deliberately designed to not have a testable behavior at all.

There would be very little value in unit testing these types at all - again, diminishing returns in unit testing these.

The forthcoming integration tests will cover the use of these types, and ensure they are all wired up correctly.

> Furthermore, the code analysis rules included in this codebase will ensure you declare these types correctly.

#### Request validators

Each of the validators for each of these request types (i.e., `RegisterCarRequestValidator`)

All request validators SHOULD DEFINITELY be unit-tested.

These validators require unit testing since it is highly unlikely that any other kind of test will be ensuring they work as you designed them for all permutations of data.

Your integration tests are likely to only test a small number of permutations of test data (and certainly you would not integration-tests for all permutations).

You should write a separate class for each validator and make sure you thoroughly cover both happy path cases and invalid cases.

If you have defined a custom validators, they should be independently tested as well.

There is a common testing pattern for all validators in the codebase, that you can and SHOULD copy into your tests.

It involves setting up a valid request (with the minimum amount of data allowed) and then running the test named
`WhenAllProperties_ThenSuccess`:

For example,

```c#  
[Trait("Category", "Unit")]
public class RegisterCarRequestValidatorSpec
{
    private readonly RegisterCarRequest _dto;
    private readonly RegisterCarRequestValidator _validator;

    public RegisterCarRequestValidatorSpec()
    {
        _validator = new RegisterCarRequestValidator();
        _dto = new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = Validations.Car.Year.Min,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    ... tests for all other invalid cases
}
```

### Integration testing

Lastly, we strongly recommend writing at lest one integration test per service operation that you define.

You NEED at least one integration test to cover and verify that all your layers are wired up correctly and that they are working together.

Then we recommend that you write a handful of integration tests to make sure that the known common use cases you designed for work as you expected.

This is the best approach to build your confidence that your API works end to end.

It is more effort for you, until you hit a production issue, and have to debug something simple that could have easily been avoided with an integration test.

> Once you get used to this process, you will find that you no longer need to run HTTP testing tools like Postman, and can just rely on running integration tests. You will also spend significantly less time running the code locally in the debugger and identifying issues. You will literally save hours of work in your day this way.

The number of additional integration tests that you will write here will depend on the complexity of each API call.

> Note: It is these integration tests that you will start writing more of to cover these cases when you encounter a bug in production. The process goes like this: You learn about the bug, you write a new integration test for the API that is the source of the bug, and you use production data if necessary to reproduce the bug. Then you debug and identify the root cause, and then you replace the production data with regular test data to reproduce the error. Now you are left with an extra integration test to verify that you actually fixed the issue. And it stays there for years.

We also recommend that you write these tests BEFORE you implement the code in the Application and Domain Layers. At the point just after you have implemented the Application method that your service operations delegates to, to get the code to compile.

We recommend that you get that Application method to throw a `new NotImplementedException()` and leave it as that.

Then write the integration tests to test calling the API endpoint, and start with a failing integration tests, before you go and fix the code to succeed.

The last benefit here, is that when you finish the implementation of the Application and Domain layer code, you have an integration test to confirm that you are done. Sometimes, this test will even challenge your implemented design, as the test likely expected a different result than what you ended up implementing in those lower layers, and now you have a prompt (a breadcrumb) to follow to improving your design.

Your integration tests should be written into the
`Infrastructure.IntegrationTests` project in your subdomain. For example, the
`CarsInfrastructure.IntegrationTests` project for the `CarsApi`.

Create that project in the `Tests` solution folder, using the `SaaStack Integration Test` project template.

Then rename the included tests class to reflect the name of your API class.

For example, `CarsApiSpec.cs`

These tests follow a common pattern that runs your entire codebase in a testing host (ASPNET
`TestServer`) and gives you access to a typed testing client for calling your API very easily.

For example,

```c#
[Trait("Category", "Integration.API")]
[Collection("API")]
public class CarsApiSpec : WebApiSpec<Program>
{
    public CarsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenRegisterCar_ThenReturnsCar()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new RegisterCarRequest
        {
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = 2023,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var car = result.Content.Value.Car!;
        var location = result.Headers.Location?.ToString();
        location.Should().Be(new GetCarRequest { Id = car.Id }.ToUrl());
        car.Id.Should().NotBeEmpty();
        car.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        car.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        car.Manufacturer!.Year.Should().Be(2023);
        car.Plate!.Jurisdiction.Should().Be(Jurisdiction.AllowedCountries[0]);
        car.Plate!.Number.Should().Be("aplate");
        car.Owner!.Id.Should().Be(login.User.Id);
        car.Managers![0].Id.Should().Be(login.User.Id);
        car.Status.Should().Be(CarStatus.Registered.ToString());
    }
    
    ... other tests
        
        
    private static void OverrideDependencies(IServiceCollection services)
    {
        // override any dependencies with stubs
    }
}
```

Notice a few key things here:

1. The class derives from
   `WebApiSpec<Program>` this is necessary to launch your API code into a test harness for testing APIs.
2. The class is decorated with the category
   `[Trait("Category", "Integration.API")]` that is important to distinguish these kinds of tests from others (like unit tests).
3. The class is decorated with the attribute
   `[Collection("API")]` this is very important so that your integration tests do not interfere with the data of any other test running at the same time. Actually, this attribute prevents them from running in parallel, which would be disastrous for many tests.
4. We call the
   `EmptyAllRepositories()` so that all test data is destroyed before each test is run. This is an important testing strategy, where a test itself is responsible for setting up it own testing context (and its data) for itself to work - then that data is destroyed before the next test runs.
5. Your API may be designed for authenticated users, and given that they will not exist before your tests runs, you might need to register and authenticate a new user for each test. You would do that by calling code like
   `var login = await LoginUserAsync();` which makes all the API calls to register a new user and setup their account, and provides the rest of your test a token that can be used to identify this user.
6. Each test should provide appropriate (minimum) test data to run properly. We have patterns for the naming of that data right across the codebase. Please follow it, and don't reinvent the wheel.
7. Finally, you should be going to extra lengths to verify in detail each and every piece of the response of the API call you are testing (not necessarily the responses of other APIs you are using to test your API). This test code covers the code you many not have written unit tests for in other layers. If you fail to do this well, you will end up with very subtle bugs in HTTP responses from your API, from the mapping code at several layers in the call stack. This will be very annoying given all the work you have put into that code, and how easily that class of bug could have been mitigated against in a test like this.

#### Stubbed services

Occasionally, depending on the subdomain that you are building, you may be using a port and adapter to a service (i.e, external 3rd party service) that is hard to run your tests with.

> We should always be striving to run our integration tests in any environment, with almost zero environmental setup. Anything that requires a change to that environment should be considered as an impediment to automated testing.

For example, lets say you are using a port to send notification to user by email.

In integration testing we certainly don't want to be sending real emails to anyone.

> There are many very good reasons for not doing this in testing, see [this guide](100-build-adapter-third-party.md) for more details on why.

The bottom line here is that you might have to swap out the real adapter, for a "stub adapter" during integration testing.

> This is actually done for you for a number of adapters already, in all your integration tests, by the testing harness already

Let's say that you are using the following application interface in your Application layer:
`ICustomService`, and let's say that you have already built a technology adapter for that interface called
`MyCustomService`. This adapter is likely to have been injected at runtime, into your subdomain module at runtime. So, when your integration tests run, that adapter is being used for real in automated testing.

We want to avoid using the real adapter, so we need to build an integration testing stub adapter instead.

In the integrations testing project of your infrastructure project (where your integration tests are), you will create a sub-folder called
`Stubs`.

Then you will create a new sealed class named for example: `StubCustomService` and derive it from `ICustomerService`.

You will then implement that interface, and instead of issuing calls to any other components, you can just keep track of the data that comes through the interface for checking in your integration tests.

For example,

```c# 
public sealed class StubCustomService : ICustomService
{
    public void DoSomething(string userId, string data)
    {
        LastDoneUserId = userId;
        LastDoneData = data;
    }
    
    publiv void Reset()
    {
        LastDoneUserId = null;
        LastDoneData = null;
    }
    
    public string? LastDoneUserId { get; private set; }

    public string? LastDoneData { get; private set; }
}
```

Now, to use this stub, in your integration tests you simply override the real adapter, with the sub adapter in the
`OverrideDependencies()` method.

```c#
[Trait("Category", "Integration.API")]
[Collection("API")]
public class CarsApiSpec : WebApiSpec<Program>
{
    private readonly StubCustomService _customService;
    
    public CarsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _customService =
            setup.GetRequiredService<ICustomService>().As<StubCustomService>();
        _customService.Reset();
    }

    [Fact]
    public async Task WhenRegisterCar_ThenReturnsCar()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new RegisterCarRequest
        {
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = 2023,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var car = result.Content.Value.Car!;
        var location = result.Headers.Location?.ToString();
        location.Should().Be(new GetCarRequest { Id = car.Id }.ToUrl());
        car.Id.Should().NotBeEmpty();
        car.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        car.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        car.Manufacturer!.Year.Should().Be(2023);
        car.Plate!.Jurisdiction.Should().Be(Jurisdiction.AllowedCountries[0]);
        car.Plate!.Number.Should().Be("aplate");
        car.Owner!.Id.Should().Be(login.User.Id);
        car.Managers![0].Id.Should().Be(login.User.Id);
        car.Status.Should().Be(CarStatus.Registered.ToString());
        
        _customService.LastDoneUserId.Should().Be("auserid");
    }
    
    ... other tests
        
        
    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddPerHttpRequest<ICustomService, StubCustomService>();
    }
}
```

You can then get a reference to your stub in the constructor of the test, and call the
`Reset()` method to clear the context of the stub adapter.

Finally, in your test, (as you can see above) you can ask the stub adapter if it was used and what data was used, if you needed to know that detail.

#### TESTINGONLY

Last thing to mention with integration-testing your APIs.

Sometimes, to set up the data and context that you need to test one of your APIs, you may find that you cannot do that, either:

* Creating the right data and context (i.e., you are missing a necessary API)
* OR you cannot test the outcome you want effectively enough (i.e., the necessary API is missing to show you that data).

What you might need to do is build a TESTINGONLY API to solve this problem.

A TESTINGONLY API is an endpoint (and downstream code) that you only want to build in non-production environments. It is not part of the product you ae building, but you need it for effective testing.

This codebase has a lot of examples of such APIs.

We recommend the use of these TESTINGONLY APIs, given the comprehensive [integration testing strategies](../design-principles/0190-testing-strategies.md) we are using.

For a good example, look at how this API is defined: `PasswordCredentialsApi.GetConfirmationToken`.

Look at how the service operation is defined, and how the `GetRegistrationPersonConfirmationRequest` is defined.

Navigate down the code path down from the API service operation, and down through the Application (
`PasswordCredentialsApplication.GetPersonRegistrationConfirmationAsync()`) to the Domain layer.

You will see the disciplined use of the
`#if TESTINGONLY` compiler directive, to ensure that none of this code reaches a production server - ever. After all, if this specific API were ever exposed to the public, it would seriously compromise entirely all security measures that have been build in!

> However, it is an extremely useful API in testing, to avoid having to scrape a database for a specific value that is not exposed in any other interface!
