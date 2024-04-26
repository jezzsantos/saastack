using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request filter that rewrites the tenant ID into the request parameter
///     of the current <see cref="RequestDelegate" /> of the current EndPoint, if it is either:
///     1. a <see cref="ITenantedRequest" />
///     2. a <see cref="IUnTenantedOrganizationRequest" />
///     but no rewrite for an untenanted request
/// </summary>
public class MultiTenancyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var tenancyContext = context.HttpContext.RequestServices.GetRequiredService<ITenancyContext>();
        var cancellationToken = context.HttpContext.RequestAborted;

        var result = await ModifyRequestAsync(context, tenancyContext, cancellationToken);
        if (result.IsFailure)
        {
            var httpError = result.Error.ToHttpError();
            return Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
        }

        return await next(context); //Continue down the pipeline
    }

    // ReSharper disable once UnusedParameter.Local
    private static async Task<Result<Error>> ModifyRequestAsync(
        EndpointFilterInvocationContext filterContext, ITenancyContext tenancyContext,
        CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        var requestDto = GetRequestDtoFromEndpoint(filterContext);
        if (!requestDto.HasValue)
        {
            return Result.Ok;
        }

        var tenantId = tenancyContext.Current;
        if (tenantId.HasNoValue())
        {
            return Result.Ok;
        }

        if (requestDto.Value is ITenantedRequest tenantedRequest)
        {
            if (tenantedRequest.OrganizationId.HasNoValue())
            {
                tenantedRequest.OrganizationId = tenantId;
            }
        }

        if (requestDto.Value is IUnTenantedOrganizationRequest unTenantedOrganizationRequest)
        {
            if (unTenantedOrganizationRequest.Id.HasNoValue())
            {
                unTenantedOrganizationRequest.Id = tenantId;
            }
        }

        return Result.Ok;
    }

    private static Optional<IWebRequest> GetRequestDtoFromEndpoint(EndpointFilterInvocationContext filterContext)
    {
        var requestHandlerParameters = filterContext.Arguments;
        if (requestHandlerParameters.Count != 2)
        {
            return Optional<IWebRequest>.None;
        }

        var requestDto = filterContext.Arguments[1];
        if (requestDto is IWebRequest webRequest)
        {
            return webRequest.ToOptional();
        }

        return Optional<IWebRequest>.None;
    }
}