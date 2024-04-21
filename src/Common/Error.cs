#if !ANALYZERS_NONPLATFORM
using Common.Extensions;
#endif

namespace Common;

/// <summary>
///     Defines a <see cref="Result" /> error, used for result return values
/// </summary>
public readonly struct Error
{
    public const string NoErrorMessage = "unexplained";

    public Error()
    {
        Code = ErrorCode.NoError;
        Message = NoErrorMessage;
    }

    internal Error(ErrorCode code, string? message = null)
    {
        Code = code;
        Message = message ?? NoErrorMessage;
    }

    public string Message { get; }

    public ErrorCode Code { get; }

#if !ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Wraps the existing message within the specified message
    /// </summary>
    public Error Wrap(string message)
    {
        if (message.HasNoValue())
        {
            return new Error(Code, Message);
        }

        return new Error(Code, Message.HasValue() && Message != NoErrorMessage
            ? $"{message}{Environment.NewLine}\t{Message}"
            : message);
    }

    /// <summary>
    ///     Wraps the existing message within the specified message, for the specified code
    /// </summary>
    public Error Wrap(ErrorCode code, string message)
    {
        if (message.HasNoValue())
        {
            return new Error(code, Message);
        }

        return new Error(code, Message.HasValue() && Message != NoErrorMessage
            ? $"{Code}: {message}{Environment.NewLine}\t{Message}"
            : $"{Code}: {message}");
    }
#endif

    /// <summary>
    ///     Whether this error is of the specified <see cref="code" />
    ///     and optional <see cref="message" />
    /// </summary>
    public bool Is(ErrorCode code, string? message = null)
    {
        return code == Code && (message == null || message == Message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NoError" /> error
    /// </summary>
    public static Error NoError => new(ErrorCode.NoError);

    /// <summary>
    ///     Creates a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static Error Validation(string? message = null)
    {
        return new Error(ErrorCode.Validation, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RuleViolation" /> error
    /// </summary>
    public static Error RuleViolation(string? message = null)
    {
        return new Error(ErrorCode.RuleViolation, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RoleViolation" /> error
    /// </summary>
    public static Error RoleViolation(string? message = null)
    {
        return new Error(ErrorCode.RoleViolation, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.PreconditionViolation" /> error
    /// </summary>
    public static Error PreconditionViolation(string? message = null)
    {
        return new Error(ErrorCode.PreconditionViolation, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityNotFound" /> error
    /// </summary>
    public static Error EntityNotFound(string? message = null)
    {
        return new Error(ErrorCode.EntityNotFound, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityExists" /> error
    /// </summary>
    public static Error EntityExists(string? message = null)
    {
        return new Error(ErrorCode.EntityExists, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NotAuthenticated" /> error
    /// </summary>
    public static Error NotAuthenticated(string? message = null)
    {
        return new Error(ErrorCode.NotAuthenticated, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.ForbiddenAccess" /> error
    /// </summary>
    public static Error ForbiddenAccess(string? message = null)
    {
        return new Error(ErrorCode.ForbiddenAccess, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NotSubscribed" /> error
    /// </summary>
    public static Error NotSubscribed(string? message = null)
    {
        return new Error(ErrorCode.NotSubscribed, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.Unexpected" /> error
    /// </summary>
    public static Error Unexpected(string? message = null)
    {
        return new Error(ErrorCode.Unexpected, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityDeleted" /> error
    /// </summary>
    public static Error EntityDeleted(string? message = null)
    {
        return new Error(ErrorCode.EntityDeleted, message);
    }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}

/// <summary>
///     Defines the common types (codes) of errors that can happen in code at any layer,
///     that return <see cref="Result{Error}" />
/// </summary>
public enum ErrorCode
{
    // EXTEND: add other kinds of errors you want to support in Result<TError>
    NoError = -1,
    Validation,
    RuleViolation,
    RoleViolation,
    PreconditionViolation,
    EntityNotFound,
    EntityExists,
    NotAuthenticated,
    ForbiddenAccess,
    NotSubscribed,
    Unexpected,
    EntityDeleted
}