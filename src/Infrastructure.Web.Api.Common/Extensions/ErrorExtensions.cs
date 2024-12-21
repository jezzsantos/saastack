using Common;
using Common.Extensions;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ErrorExtensions
{
    /// <summary>
    ///     Converts the specified error to a <see cref="IResult" /> with problem details according to
    ///     <see href="https://datatracker.ietf.org/doc/html/rfc7807" />
    /// </summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var httpError = error.ToHttpError();
        return (ProblemHttpResult)Results.Problem(title: error.AdditionalCode.HasValue()
                ? error.AdditionalCode
                : null, statusCode: (int)httpError.Code, detail: httpError.Message,
            extensions: httpError.AdditionalData!);
    }

    /// <summary>
    ///     Converts the <see cref="Error" /> to a <see cref="HttpError" />
    /// </summary>
    private static HttpError ToHttpError(this Error error)
    {
        var httpStatusCode = HttpConstants.StatusCodes.SupportedErrorCodesMap
            .FirstOrDefault(c => c.Value.Contains(error.Code));
        if (httpStatusCode.NotExists())
        {
            return new HttpError(HttpErrorCode.InternalServerError, error.Message, error.AdditionalData);
        }

        return new HttpError(httpStatusCode.Key.ToStatusCode().HttpErrorCode ?? HttpErrorCode.InternalServerError,
            error.Message, error.AdditionalData);
    }
}