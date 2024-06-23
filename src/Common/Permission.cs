using Common.Extensions;

namespace Common;

/// <summary>
///     Defines a permission given for an action
/// </summary>
public class Permission
{
    public static readonly Permission Allowed = new(true, null, PermissionResult.Allowed);

    private Permission(bool isAllowed, string? disallowedReason, PermissionResult result)
    {
        IsAllowed = isAllowed;
        DisallowedReason = disallowedReason ?? Resources.Permission_Disallowed;
        Result = result;
    }

    public string DisallowedReason { get; }

    public bool IsAllowed { get; }

    public bool IsDenied => !IsAllowed;

    public PermissionResult Result { get; }

    public static Permission Denied_Evaluating(Error error)
    {
        return new Permission(false, error.Message, PermissionResult.Rule);
    }

    public static Permission Denied_Role(string reason)
    {
        return new Permission(false, reason, PermissionResult.Role);
    }

    public static Permission Denied_Rule(string reason)
    {
        return new Permission(false, reason, PermissionResult.Rule);
    }

    /// <summary>
    ///     Converts the permission <see cref="Result" /> to an error,
    ///     using the specified formatted message (if provided),
    /// </summary>
    public Error ToError(string? formattedMessage = null)
    {
        var message = formattedMessage.HasValue()
            ? formattedMessage.Format(DisallowedReason)
            : DisallowedReason;

        return Result switch
        {
            PermissionResult.Rule => Error.RuleViolation(message),
            PermissionResult.Role => Error.RoleViolation(message),
            _ => Error.RuleViolation(message)
        };
    }
}

/// <summary>
///     Defines the result of the permission
/// </summary>
public enum PermissionResult
{
    Allowed = 0,
    Rule = 1,
    Role = 2
}