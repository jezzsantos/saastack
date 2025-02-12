using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request filter that rewrites the tenant ID into the current request DTO
///     of the current <see cref="RequestDelegate" /> of the current EndPoint, if it is either:
///     1. a <see cref="ITenantedRequest" />
///     2. a <see cref="IUnTenantedOrganizationRequest" />
///     There is no rewrite for any untenanted request
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
            return result.Error.ToProblem();
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
        var requestDto = filterContext.GetRequestDto();
        if (requestDto.NotExists())
        {
            return Result.Ok;
        }

        var tenantId = tenancyContext.Current;
        if (tenantId.HasNoValue())
        {
            return Result.Ok;
        }

        if (requestDto is ITenantedRequest tenantedRequest)
        {
            if (tenantedRequest.OrganizationId.HasNoValue())
            {
                tenantedRequest.OrganizationId = tenantId;
            }
        }

        if (requestDto is IUnTenantedOrganizationRequest unTenantedOrganizationRequest)
        {
            if (unTenantedOrganizationRequest.Id.HasNoValue())
            {
                unTenantedOrganizationRequest.Id = tenantId;
            }
        }

        return Result.Ok;
    }
}