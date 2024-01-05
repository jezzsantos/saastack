using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available platform scoped roles
///     (i.e. access to un-tenanted resources by end users)
/// </summary>
public static class PlatformRoles
{
    public const string ExternalWebhookService = "external_webhook_service";
    public const string ServiceAccount = "service";

    // EXTEND: Add other roles that EndUsers can be assigned to control un-tenanted resources
    public const string Standard = "standard";

#if TESTINGONLY
    public const string TestingOnlyUser = "testingonly_user";
#endif
    private static readonly IReadOnlyList<string> PlatformAssignableRoles = new List<string>
    {
        // EXTEND: Add roles above that can be assigned by other endusers, to control access to un-tenanted resources  
        Standard,

#if TESTINGONLY
        TestingOnlyUser
#endif
    };

    /// <summary>
    ///     Whether the <see cref="role" /> is an assignable role
    /// </summary>
    public static bool IsPlatformAssignableRole(string role)
    {
        return PlatformAssignableRoles.ContainsIgnoreCase(role);
    }
}