using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available platform scoped roles
///     (i.e. access to untenanted resources by any end-user)
/// </summary>
public static class PlatformRoles
{
    public static readonly RoleLevel ExternalWebhookService = new("external_webhook_service");
    public static readonly RoleLevel Operations = new("operations");
    public static readonly RoleLevel ServiceAccount = new("service");
    public static readonly RoleLevel Standard = new("standard");
    public static readonly RoleLevel TestingOnly = new("testingonly_platform");
    public static readonly RoleLevel TestingOnlySuperUser = new("testingonly_platform_super", TestingOnly);
    public static readonly Dictionary<string, RoleLevel> AllRoles = new()
    {
        { Standard.Name, Standard },
        { Operations.Name, Operations },
        { ServiceAccount.Name, ServiceAccount },
        { ExternalWebhookService.Name, ExternalWebhookService },
#if TESTINGONLY
        { TestingOnly.Name, TestingOnly },
        { TestingOnlySuperUser.Name, TestingOnlySuperUser }
#endif
    };
    // EXTEND: Add other roles to control access to untenanted resources (e.g. untenanted APIs)

    private static readonly IReadOnlyList<RoleLevel> PlatformAssignableRoles = new List<RoleLevel>
    {
        // EXTEND: Add new roles that can be assigned/unassigned to EndUsers
        Standard,
        Operations,

#if TESTINGONLY
        TestingOnly,
        TestingOnlySuperUser
#endif
    };

    /// <summary>
    ///     Returns the <see cref="RoleLevel" /> for the specified <see cref="name" /> of the role
    /// </summary>
    public static RoleLevel? FindRoleByName(string name)
    {
#if NETSTANDARD2_0
        return AllRoles.TryGetValue(name, out var role)
            ? role
            : null;
#else
        return AllRoles.GetValueOrDefault(name);
#endif
    }

    /// <summary>
    ///     Whether the <see cref="role" /> is assignable
    /// </summary>
    public static bool IsPlatformAssignableRole(string role)
    {
        return PlatformAssignableRoles
            .Select(rol => rol.Name)
            .ContainsIgnoreCase(role);
    }
}