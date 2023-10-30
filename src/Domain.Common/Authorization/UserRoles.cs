using Common.Extensions;

namespace Domain.Common.Authorization;

/// <summary>
///     Defines the available user scoped roles (access to un-tenanted resources)
/// </summary>
public static class UserRoles
{
    public const string ExternalWebhookService = "external_webhook_service";
    public const string ServiceAccount = "service";

    // EXTEND: Add other roles that  UserAccounts can be assigned to control un-tenanted resources
    public const string Standard = "standard";

#if TESTINGONLY
    public const string TestingOnlyUser = "testingonly_user";
#endif
    private static readonly IReadOnlyList<string> UserAssignableRoles = new List<string>
    {
        // EXTEND: Add roles above that can be assigned by other users, to control access to un-tenanted resources  
        Standard,

#if TESTINGONLY
        TestingOnlyUser
#endif
    };

    /// <summary>
    ///     Whether the <see cref="role" /> is an assignable role
    /// </summary>
    public static bool IsUserAssignableRole(string role)
    {
        return UserAssignableRoles.ContainsIgnoreCase(role);
    }
}