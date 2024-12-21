using System.Net;
using FluentValidation.Results;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ValidationResultExtensions
{
    /// <summary>
    ///     Converts <see cref="ValidationResult" /> to an
    ///     <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC7807 error</see>
    /// </summary>
    public static ProblemDetails ToRfc7807(this ValidationResult result, string requestUrl)
    {
        var validationDetails = result.Errors.Select(error => new ValidatorProblem
            {
                Rule = error.ErrorCode,
                Reason = error.ErrorMessage,
                Value = error.AttemptedValue
            })
            .ToList();
        var firstMessage = result.Errors.Select(error => error.ErrorMessage)
            .First();

        var details = new ProblemDetails
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5",
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = firstMessage,
            Instance = requestUrl
        };
        details.Extensions.Add(HttpConstants.Responses.ProblemDetails.Extensions.ValidationErrorPropertyName,
            validationDetails);

        return details;
    }
}