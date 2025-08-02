#if !ANALYZERS_NONPLATFORM && !GENERATORS_WORKERS_PROJECT
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
        AdditionalData = null;
        AdditionalCode = null;
    }

    internal Error(ErrorCode code, string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        Code = code;
        Message = message ?? NoErrorMessage;
        AdditionalData = additionalData;
        AdditionalCode = additionalCode;
    }

    public string Message { get; }

    public Dictionary<string, object>? AdditionalData { get; }

    public ErrorCode Code { get; }

    public string? AdditionalCode { get; }

#if !ANALYZERS_NONPLATFORM && !GENERATORS_WORKERS_PROJECT
    /// <summary>
    ///     Wraps the existing message within the specified message
    /// </summary>
    public Error Wrap(string message, Dictionary<string, object>? additionalData = null)
    {
        var additional = AdditionalData;
        if (additionalData.Exists())
        {
            if (AdditionalData.Exists())
            {
                AdditionalData.Merge(additionalData);
                additional = AdditionalData;
            }
            else
            {
                additional = additionalData;
            }
        }

        if (message.HasNoValue())
        {
            return new Error(Code, Message, additionalData: additional);
        }

        return new Error(Code, Message.HasValue() && Message != NoErrorMessage
            ? $"{message}{Environment.NewLine}\t{Message}"
            : message, additionalData: additional);
    }

    /// <summary>
    ///     Wraps the existing message within the specified message, for the specified code
    /// </summary>
    public Error Wrap(ErrorCode code, string message, Dictionary<string, object>? additionalData = null)
    {
        var additional = AdditionalData;
        if (additionalData.Exists())
        {
            if (AdditionalData.Exists())
            {
                AdditionalData.Merge(additionalData);
                additional = AdditionalData;
            }
            else
            {
                additional = additionalData;
            }
        }

        if (message.HasNoValue())
        {
            return new Error(code, Message, additionalData: additional);
        }

        return new Error(code, Message.HasValue() && Message != NoErrorMessage
            ? $"{Code}: {message}{Environment.NewLine}\t{Message}"
            : $"{Code}: {message}", additionalData: additional);
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
    ///     Whether this error is notof the specified <see cref="code" />
    ///     and optional <see cref="message" />
    /// </summary>
    public bool IsNot(ErrorCode code, string? message = null)
    {
        return !Is(code, message);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NoError" /> error
    /// </summary>
    public static Error NoError => new(ErrorCode.NoError);

    /// <summary>
    ///     Creates a <see cref="ErrorCode.Validation" /> error
    /// </summary>
    public static Error Validation(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.Validation, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RuleViolation" /> error
    /// </summary>
    public static Error RuleViolation(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.RuleViolation, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RoleViolation" /> error
    /// </summary>
    public static Error RoleViolation(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.RoleViolation, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.PreconditionViolation" /> error
    /// </summary>
    public static Error PreconditionViolation(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.PreconditionViolation, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityNotFound" /> error
    /// </summary>
    public static Error EntityNotFound(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityNotFound, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityExists" /> error
    /// </summary>
    public static Error EntityExists(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityExists, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityLocked" /> error
    /// </summary>
    public static Error EntityLocked(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityLocked, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NotAuthenticated" /> error
    /// </summary>
    public static Error NotAuthenticated(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.NotAuthenticated, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.ForbiddenAccess" /> error
    /// </summary>
    public static Error ForbiddenAccess(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.ForbiddenAccess, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.FeatureViolation" /> error
    /// </summary>
    public static Error FeatureViolation(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.FeatureViolation, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.Unexpected" /> error
    /// </summary>
    public static Error Unexpected(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.Unexpected, message, additionalCode, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityDeleted" /> error
    /// </summary>
    public static Error EntityDeleted(string? message = null, string? additionalCode = null,
        Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityDeleted, message, additionalCode, additionalData);
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
    PreconditionViolation, // the resource is not in a valid state to begin with
    EntityNotFound,
    EntityExists,
    EntityLocked,
    EntityDeleted,
    NotAuthenticated,
    ForbiddenAccess,
    FeatureViolation,
    Unexpected
}