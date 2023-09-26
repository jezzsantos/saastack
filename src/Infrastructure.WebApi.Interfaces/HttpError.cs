namespace Infrastructure.WebApi.Interfaces;

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

public enum HttpErrorCode
{
    BadRequest = 400,
    Unauthorized = 401,
    PaymentRequired = 402,
    Forbidden = 403,
    NotFound = 404,
    MethodNotAllowed = 405,
    Conflict = 409,
    InternalServerError = 500
}