using Domain.Common.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class OrganizationRolesSpec
{
    [Fact]
    public void WhenIsMemberAssignableRoleForUnknownRole_ThenReturnsFalse()
    {
        var result = OrganizationRoles.IsMemberAssignableRole("arole");

        result.Should().BeFalse();
    }
#if TESTINGONLY

    [Fact]
    public void WhenIsMemberAssignableRoleForAssignableRole_ThenReturnsTrue()
    {
        var result = OrganizationRoles.IsMemberAssignableRole(OrganizationRoles.TestingOnlyOrganization);

        result.Should().BeTrue();
    }
#endif
}