using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.WebApi.Interfaces;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Common;

[UsedImplicitly]
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, IResult>
    where TRequest : IWebRequest<TResponse>
    where TResponse : IWebResponse
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

public static class ValidationConversionExtensions
{
    /// <summary>
    ///     Converts <see cref="ValidationResult" /> to an RFC7807 error
    /// </summary>
    public static ProblemDetails ToRfc7807(this ValidationResult result, string requestUrl)
    {
        var validationDetails = result.Errors
            .Select(error => new
            {
                Rule = error.ErrorCode,
                Reason = error.ErrorMessage,
                Value = error.AttemptedValue
            }).ToList();
        var firstMessage = result.Errors.Select(error => error.ErrorMessage).First();
        var firstCode = result.Errors.Select(error => error.ErrorCode).First();

        var details = new ProblemDetails
        {
            Type = firstCode,
            Title = Resources.ValidationBehavior_ErrorTitle,
            Status = (int)HttpStatusCode.BadRequest,
            Detail = firstMessage,
            Instance = requestUrl
        };
        details.Extensions.Add("errors", validationDetails);

        return details;
    }
}