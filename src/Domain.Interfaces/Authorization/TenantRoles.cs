using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available organization member scoped roles
///     (i.e. access to tenanted resources by members of organizations)
/// </summary>
public static class TenantRoles
{
    public static readonly RoleLevel Member = new("member");
    public static readonly RoleLevel Owner = new("member_owner", Member);
    public static readonly RoleLevel BillingAdmin = new("billing_admin", Owner);
    public static readonly RoleLevel TestingOnly = new("testingonly_tenant");
    public static readonly Dictionary<string, RoleLevel> AllRoles = new()
    {
        { Member.Name, Member },
        { Owner.Name, Owner },
        { BillingAdmin.Name, BillingAdmin },
#if TESTINGONLY
        { TestingOnly.Name, TestingOnly },
#endif
    };

    // EXTEND: Add other roles that end-users can be assigned to control access to tenanted resources (e.g. tenanted APIs)

    private static readonly IReadOnlyList<RoleLevel> TenantAssignableRoles = new List<RoleLevel>
    {
        // EXTEND: Add new roles that can be assigned/unassigned to EndUsers
        Member,
        Owner,
        BillingAdmin,

#if TESTINGONLY
        TestingOnly
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
    ///     Whether the specified <see cref="role" /> is assignable
    /// </summary>
    public static bool IsTenantAssignableRole(string role)
    {
        return TenantAssignableRoles
            .Select(rol => rol.Name)
            .ContainsIgnoreCase(role);
    }
}