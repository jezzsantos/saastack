using Domain.Interfaces.Authorization;

namespace Application.Interfaces.Extensions;

public static class CallContextExtensions
{
    /// <summary>
    ///     Whether the caller is a privileged operations user
    /// </summary>
    public static bool IsOperations(this ICallerContext callerContext)
    {
        return callerContext.Roles.Platform.Contains(PlatformRoles.Operations);
    }
}