using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request filter that validates the request with the respective Fluent Validation validator
/// </summary>
public class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : IWebRequest
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requestDto = context.GetRequestDto();
        if (requestDto.NotExists())
        {
            return await next(context); //Continue down the pipeline
        }

        if (requestDto is not TRequest webRequest)
        {
            return await next(context); //Continue down the pipeline
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator.NotExists())
        {
            return await next(context); //Continue down the pipeline
        }

        var validationResult = await validator.ValidateAsync(webRequest);
        if (validationResult.IsValid)
        {
            return await next(context); //Continue down the pipeline
        }

        var requestUrl = context.HttpContext.Request.GetDisplayUrl();
        return Results.Problem(validationResult.ToRfc7807(requestUrl));
    }
}