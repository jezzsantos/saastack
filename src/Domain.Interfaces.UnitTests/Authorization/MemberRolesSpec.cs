using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class MemberRolesSpec
{
    [Fact]
    public void WhenIsMemberAssignableRoleForUnknownRole_ThenReturnsFalse()
    {
        var result = MemberRoles.IsMemberAssignableRole("arole");

        result.Should().BeFalse();
    }
#if TESTINGONLY

    [Fact]
    public void WhenIsMemberAssignableRoleForAssignableRole_ThenReturnsTrue()
    {
        var result = MemberRoles.IsMemberAssignableRole(MemberRoles.TestingOnlyTenant);

        result.Should().BeTrue();
    }
#endif
}