using System.Net;
using FluentValidation.Results;
using Infrastructure.WebApi.Common.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Common;

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
        var firstCode = result.Errors.Select(error => error.ErrorCode)
            .First();

        var details = new ProblemDetails
        {
            Type = firstCode,
            Title = ValidationResources.ValidationBehavior_ErrorTitle,
            Status = (int)HttpStatusCode.BadRequest,
            Detail = firstMessage,
            Instance = requestUrl
        };
        details.Extensions.Add("errors", validationDetails);

        return details;
    }
}