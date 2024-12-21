using System.Net;
using Common;

namespace Infrastructure.Web.Interfaces;

/// <summary>
///     Defines an error for using in <see cref="Result{TError}" /> return types
/// </summary>
public struct HttpError
{
    public HttpError(HttpErrorCode code, string? message = null, Dictionary<string, object>? additionalData = null)
    {
        Message = message;
        Code = code;
        AdditionalData = additionalData;
    }

    public HttpErrorCode Code { get; }

    public string? Message { get; }

    public Dictionary<string, object>? AdditionalData { get; }
}

/// <summary>
///     Defines the commonly used HTTP StatusCodes
/// </summary>
public enum HttpErrorCode
{
    //EXTEND with others from HttpConstants.StatusCodes.SupportedErrors
    BadRequest = HttpStatusCode.BadRequest,
    Unauthorized = HttpStatusCode.Unauthorized,
    PaymentRequired = HttpStatusCode.PaymentRequired,
    Forbidden = HttpStatusCode.Forbidden,
    NotFound = HttpStatusCode.NotFound,
    MethodNotAllowed = HttpStatusCode.MethodNotAllowed,
    Conflict = HttpStatusCode.Conflict,
    Locked = HttpStatusCode.Locked,
    InternalServerError = HttpStatusCode.InternalServerError
}