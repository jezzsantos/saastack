namespace Common;

public struct Error
{
    private Error(ErrorCode code, string? message = null)
    {
        Code = code;
        Message = message;
    }

    public string? Message { get; }

    public ErrorCode Code { get; }

    public static Error Validation(string? message = null)
    {
        return new Error(ErrorCode.Validation, message);
    }

    public static Error RuleViolation(string? message = null)
    {
        return new Error(ErrorCode.RuleViolation, message);
    }

    public static Error RoleViolation(string? message = null)
    {
        return new Error(ErrorCode.RoleViolation, message);
    }

    public static Error PreconditionViolation(string? message = null)
    {
        return new Error(ErrorCode.PreconditionViolation, message);
    }

    public static Error EntityNotFound(string? message = null)
    {
        return new Error(ErrorCode.EntityNotFound, message);
    }

    public static Error EntityExists(string? message = null)
    {
        return new Error(ErrorCode.EntityExists, message);
    }

    public static Error NotAuthenticated(string? message = null)
    {
        return new Error(ErrorCode.NotAuthenticated, message);
    }

    public static Error ForbiddenAccess(string? message = null)
    {
        return new Error(ErrorCode.ForbiddenAccess, message);
    }

    public static Error NotSubscribed(string? message = null)
    {
        return new Error(ErrorCode.NotSubscribed, message);
    }

    public static Error Unexpected(string? message = null)
    {
        return new Error(ErrorCode.Unexpected, message);
    }
}

public enum ErrorCode
{
    Validation,
    RuleViolation,
    RoleViolation,
    PreconditionViolation,
    EntityNotFound,
    EntityExists,
    NotAuthenticated,
    ForbiddenAccess,
    NotSubscribed,
    Unexpected
}