using FluentValidation;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Defines a <see cref="IPipelineBehavior{TRequest,IResult}" /> for validating all incoming
///     <see cref="IWebRequest{TResponse}" /> with the appropriately registered <see cref="IValidator{TRequest}" />
/// </summary>
[UsedImplicitly]
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, IResult>
    where TRequest : IWebRequest<TResponse> where TResponse : IWebResponse
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<TRequest> _validator;

    public ValidationBehavior(IValidator<TRequest> validator, IHttpContextAccessor httpContextAccessor)
    {
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IResult> Handle(TRequest request, RequestHandlerDelegate<IResult> next,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var requestUrl = _httpContextAccessor.HttpContext!.Request.GetDisplayUrl();
            return Results.Problem(validationResult.ToRfc7807(requestUrl));
        }

        return await next();
    }
}