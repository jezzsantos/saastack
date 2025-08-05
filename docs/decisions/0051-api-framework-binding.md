# API Framework Bindings

* status: proposed

* date: 2025-02-15
* deciders: jezzsantos

# Context and Problem Statement

We are using minimal APIs, and we are defining all request handlers in this form:

```c#
apiGroup.MapPut("/resources/{Id}/action",
    async (global::System.IServiceProvider serviceProvider, ARequest request) =>
    {
        return await Handle(serviceProvider, request, global::System.Threading.CancellationToken.None);

        static async Task<global::Microsoft.AspNetCore.Http.IResult> Handle(global::System.IServiceProvider services, ARequest request, global::System.Threading.CancellationToken cancellationToken)
        {
            var callerFactory = services.GetRequiredService<Infrastructure.Interfaces.ICallerContextFactory>();
            var anApplication = services.GetRequiredService<IAnApplication>();

            var api = new AnApi(callerFactory, anApplication);
            var result = await api.AUseCase(request, cancellationToken);
            return result.HandleApiResult(global::Infrastructure.Web.Api.Interfaces.OperationMethod.PutPatch);
        }
    })
```

Where `ARequest` could look like this:

```c#
[Route("/resources/{Id}/action", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ARequest : TenantedRequest<ARequest, AResponse>
{
    [Required] public string? Id { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }
}
```

Where all request data is expected by the API as in either: `application/json` or `multipart/form-data` or in `application/x-www-form-urlencoded`.

In this form, [traditional model binding for minimal APIs](https://learn.microsoft.com/en-gb/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0#binding-precedence) of individual properties on the `ARequest` type is very tedious for developers. Sure, it gives more control, but in most cases, it is extra work for no benefit.

The use of attributes like `[FromRoute]`, `[FromQuery]`, `[FromBody]` and  `[FromForm]` get confusing fast, and also create dependencies to ASPNET in the assemblies where the types are shared. Which increases coupling and limits their reusability for other purposes.

It is possible to provide our own customer binding, by providing a `BindAsync` method on all request types. Which we must do to support `multipart/form-data` and `application/x-www-form-urlencoded` content types if we want to avoid using most of the binding attributes.

There exist some issues with binding Enum types and case-sensitivity, and with translating camel-cased inbound JSON values using the `[JsonPropertyName("aname")]`.

There are other, separate challenges with tailoring our OpenAPI data also for readability and tooling that consumes that.

## Decision Outcome

`Custom Binding`

- We would prefer is developers did not have to use binding attributes like  `[FromRoute]`, `[FromQuery]`, `[FromBody]` and  `[FromForm]`, except in exception cases.
- We want to support both `application/json`, and `multipart/form-data` and `application/form-urlencoded`.
- We can make this easier for the developer using source generation.

### Pros and Cons of the Options

There are some drawbacks to this approach, that could be with us for some time, as the minimal API framework matures.

1. We have to define a base class for all request types, to avoid having to specify custom binding for each request type, as the ASPNET framework does not yet support a central binding override. Instead, [it only supports](https://learn.microsoft.com/en-gb/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0#binding-precedence) defining static methods on each type, and this cannot be inherited. Thus, we have had to define a base type called `WebRequest<TRequest>` that we can provide a central binding mechanism.
2. We have to specially process form data.
3. This custom binding mechanism occurs far down the request pipeline, long after middleware has run, so we need to account for it in several places.
4. In our handling of requests to support automatic handling of multi-tenancy, we have to derive the current `TenantId` of the request from many places (i.e., from query string, route values, body, etc.). We need to account for this binding there.
5. We have to override defaults in the `Swashbuckle` library that accounts for these attributes, which won't be there anymore.
