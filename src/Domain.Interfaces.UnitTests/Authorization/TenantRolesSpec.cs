using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class TenantRolesSpec
{
    [Fact]
    public void WhenIsMemberAssignableRoleForUnknownRole_ThenReturnsFalse()
    {
        var result = TenantRoles.IsTenantAssignableRole("arole");

        result.Should().BeFalse();
    }
#if TESTINGONLY

    [Fact]
    public void WhenIsMemberAssignableRoleForAssignableRole_ThenReturnsTrue()
    {
        var result = TenantRoles.IsTenantAssignableRole(TenantRoles.TestingOnly.Name);

        result.Should().BeTrue();
    }
#endif
}