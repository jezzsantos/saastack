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
        AdditionalData = null;
    }

    internal Error(ErrorCode code, string? message = null, Dictionary<string, object>? additionalData = null)
    {
        Code = code;
        Message = message ?? NoErrorMessage;
        AdditionalData = additionalData;
    }

    public string Message { get; }

    public Dictionary<string, object>? AdditionalData { get; }

    public ErrorCode Code { get; }

#if !ANALYZERS_NONPLATFORM
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
            return new Error(Code, Message, additional);
        }

        return new Error(Code, Message.HasValue() && Message != NoErrorMessage
            ? $"{message}{Environment.NewLine}\t{Message}"
            : message, additional);
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
            return new Error(code, Message, additional);
        }

        return new Error(code, Message.HasValue() && Message != NoErrorMessage
            ? $"{Code}: {message}{Environment.NewLine}\t{Message}"
            : $"{Code}: {message}", additional);
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
    public static Error Validation(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.Validation, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RuleViolation" /> error
    /// </summary>
    public static Error RuleViolation(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.RuleViolation, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.RoleViolation" /> error
    /// </summary>
    public static Error RoleViolation(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.RoleViolation, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.PreconditionViolation" /> error
    /// </summary>
    public static Error PreconditionViolation(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.PreconditionViolation, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityNotFound" /> error
    /// </summary>
    public static Error EntityNotFound(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityNotFound, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityExists" /> error
    /// </summary>
    public static Error EntityExists(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityExists, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityLocked" /> error
    /// </summary>
    public static Error EntityLocked(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityLocked, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.NotAuthenticated" /> error
    /// </summary>
    public static Error NotAuthenticated(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.NotAuthenticated, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.ForbiddenAccess" /> error
    /// </summary>
    public static Error ForbiddenAccess(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.ForbiddenAccess, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.FeatureViolation" /> error
    /// </summary>
    public static Error FeatureViolation(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.FeatureViolation, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.Unexpected" /> error
    /// </summary>
    public static Error Unexpected(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.Unexpected, message, additionalData);
    }

    /// <summary>
    ///     Creates a <see cref="ErrorCode.EntityDeleted" /> error
    /// </summary>
    public static Error EntityDeleted(string? message = null, Dictionary<string, object>? additionalData = null)
    {
        return new Error(ErrorCode.EntityDeleted, message, additionalData);
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