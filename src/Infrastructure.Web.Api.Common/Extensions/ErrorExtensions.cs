using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ErrorExtensions
{
    /// <summary>
    ///     Converts the <see cref="Error" /> to a <see cref="Infrastructure.Web.Api.Interfaces.HttpError" />
    /// </summary>
    public static HttpError ToHttpError(this Error error)
    {
        var httpStatusCode = HttpConstants.StatusCodes.SupportedErrorCodesMap
            .FirstOrDefault(c => c.Value.Contains(error.Code));
        if (httpStatusCode.NotExists())
        {
            return new HttpError(HttpErrorCode.InternalServerError, error.Message);
        }

        return new HttpError(httpStatusCode.Key.ToStatusCode().HttpErrorCode ?? HttpErrorCode.InternalServerError,
            error.Message);
    }

    /// <summary>
    ///     Converts the specified error to a <see cref="IResult" /> with a problem detail
    /// </summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var httpError = error.ToHttpError();
        return (ProblemHttpResult)Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
    }
}