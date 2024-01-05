using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available organization scoped roles
///     (i.e. access to tenanted resources by members or organizations)
/// </summary>
public static class MemberRoles
{
    public const string BillingAdmin = "member_billing_admin";

    // EXTEND: Add other roles that Memberships can be assigned to control tenanted resources
    public const string Owner = "member_owner";

#if TESTINGONLY
    public const string TestingOnlyTenant = "testingonly_organization";
#endif
    private static readonly IReadOnlyList<string> MemberAssignableRoles = new List<string>
    {
        // EXTEND: Add roles above that can be assigned by other users, to control access to tenanted resources  
        Owner,
        BillingAdmin,

#if TESTINGONLY
        TestingOnlyTenant
#endif
    };

    /// <summary>
    ///     Whether the specified <see cref="role" /> is one of the <see cref="MemberAssignableRoles" />
    /// </summary>
    public static bool IsMemberAssignableRole(string role)
    {
        return MemberAssignableRoles.ContainsIgnoreCase(role);
    }
}