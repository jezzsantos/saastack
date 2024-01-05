using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class PlatformRolesSpec
{
    [Fact]
    public void WhenIsUserAssignableRoleForUnknownRole_ThenReturnsFalse()
    {
        var result = PlatformRoles.IsPlatformAssignableRole("arole");

        result.Should().BeFalse();
    }
#if TESTINGONLY

    [Fact]
    public void WhenIsUserAssignableRoleForAssignableRole_ThenReturnsTrue()
    {
        var result = PlatformRoles.IsPlatformAssignableRole(PlatformRoles.TestingOnlyUser);

        result.Should().BeTrue();
    }
#endif
}