using Common;

namespace Infrastructure.WebApi.Interfaces;

public static class ErrorExtensions
{
    /// <summary>
    ///     Converts the <see cref="ErrorCode" /> to a <see cref="Infrastructure.WebApi.Interfaces.HttpError" />
    /// </summary>
    public static HttpError ToHttpError(this Error error)
    {
        var code = error.Code switch
        {
            ErrorCode.Unexpected => HttpErrorCode.InternalServerError,
            ErrorCode.RuleViolation => HttpErrorCode.BadRequest,
            ErrorCode.RoleViolation => HttpErrorCode.Forbidden,
            ErrorCode.PreconditionViolation => HttpErrorCode.MethodNotAllowed,
            ErrorCode.EntityNotFound => HttpErrorCode.NotFound,
            ErrorCode.EntityExists => HttpErrorCode.Conflict,
            ErrorCode.NotAuthenticated => HttpErrorCode.Unauthorized,
            ErrorCode.ForbiddenAccess => HttpErrorCode.Forbidden,
            ErrorCode.NotSubscribed => HttpErrorCode.PaymentRequired,
            _ => HttpErrorCode.InternalServerError
        };

        return new HttpError(code, error.Message);
    }
}