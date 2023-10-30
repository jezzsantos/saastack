using Common.Extensions;

namespace Domain.Common.Authorization;

/// <summary>
///     Defines the available organization scoped roles (access to tenanted resources)
/// </summary>
public static class OrganizationRoles
{
    public const string BillingAdmin = "org_billing_admin";

    // EXTEND: Add other roles that Memberships can be assigned to control tenanted resources
    public const string Owner = "org_owner";

#if TESTINGONLY
    public const string TestingOnlyOrganization = "testingonly_organization";
#endif
    private static readonly IReadOnlyList<string> MemberAssignableRoles = new List<string>
    {
        // EXTEND: Add roles above that can be assigned by other users, to control access to tenanted resources  
        Owner,
        BillingAdmin,

#if TESTINGONLY
        TestingOnlyOrganization
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