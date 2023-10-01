using System.Net;
using Common;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines an error for using in <see cref="Result{TError}" /> return types
/// </summary>
public struct HttpError
{
    public HttpError(HttpErrorCode code, string? message)
    {
        Message = message;
        Code = code;
    }

    public HttpErrorCode Code { get; }

    public string? Message { get; }
}

/// <summary>
///     Defines the commonly used HTTP StatusCodes
/// </summary>
public enum HttpErrorCode
{
    BadRequest = HttpStatusCode.BadRequest,
    Unauthorized = HttpStatusCode.Unauthorized,
    PaymentRequired = HttpStatusCode.PaymentRequired,
    Forbidden = HttpStatusCode.Forbidden,
    NotFound = HttpStatusCode.NotFound,
    MethodNotAllowed = HttpStatusCode.MethodNotAllowed,
    Conflict = HttpStatusCode.Conflict,
    InternalServerError = HttpStatusCode.InternalServerError
}