using Common;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ErrorExtensions
{
    /// <summary>
    ///     Converts the <see cref="ErrorCode" /> to a <see cref="Infrastructure.Web.Api.Interfaces.HttpError" />
    /// </summary>
    public static HttpError ToHttpError(this Error error)
    {
        var code = error.Code switch
        {
            ErrorCode.Unexpected => HttpErrorCode.InternalServerError,
            ErrorCode.RuleViolation => HttpErrorCode.BadRequest,
            ErrorCode.Validation => HttpErrorCode.BadRequest,
            ErrorCode.PreconditionViolation => HttpErrorCode.MethodNotAllowed,
            ErrorCode.RoleViolation => HttpErrorCode.Forbidden,
            ErrorCode.EntityNotFound => HttpErrorCode.NotFound,
            ErrorCode.EntityExists => HttpErrorCode.Conflict,
            ErrorCode.NotAuthenticated => HttpErrorCode.Unauthorized,
            ErrorCode.ForbiddenAccess => HttpErrorCode.Forbidden,
            ErrorCode.NotSubscribed => HttpErrorCode.PaymentRequired,
            ErrorCode.EntityDeleted => HttpErrorCode.MethodNotAllowed,
            _ => HttpErrorCode.InternalServerError
        };

        return new HttpError(code, error.Message);
    }
}