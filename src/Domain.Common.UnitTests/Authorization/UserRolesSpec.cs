using Domain.Common.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class UserRolesSpec
{
    [Fact]
    public void WhenIsUserAssignableRoleForUnknownRole_ThenReturnsFalse()
    {
        var result = UserRoles.IsUserAssignableRole("arole");

        result.Should().BeFalse();
    }
#if TESTINGONLY

    [Fact]
    public void WhenIsUserAssignableRoleForAssignableRole_ThenReturnsTrue()
    {
        var result = UserRoles.IsUserAssignableRole(UserRoles.TestingOnlyUser);

        result.Should().BeTrue();
    }
#endif
}