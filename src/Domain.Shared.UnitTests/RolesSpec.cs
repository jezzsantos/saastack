using Common;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class RoleSpec
{
    [Fact]
    public void WhenCreateWithEmpty_ThenReturnsError()
    {
        var result = Role.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = Role.Create("^aninvalidname^");

        result.Should().BeError(ErrorCode.Validation, Resources.Roles_InvalidRole);
    }

    [Fact]
    public void WhenCreateWithUnknownName_ThenReturnsValue()
    {
        var result = Role.Create("anunknownrole");

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be("anunknownrole");
    }

    [Fact]
    public void WhenCreateWithKnownName_ThenReturnsValue()
    {
        var result = Role.Create(PlatformRoles.Standard.Name);

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be(PlatformRoles.Standard.Name);
    }
}

[Trait("Category", "Unit")]
public class RolesSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsError()
    {
        var result = Roles.Create();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithSingleEmpty_ThenReturnsError()
    {
        var result = Roles.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithSingle_ThenReturnsValue()
    {
        var result = Roles.Create(PlatformRoles.Standard.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard.Name).Value);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithRoleLevel_ThenReturnsValue()
    {
        var result = Roles.Create(PlatformRoles.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(
                PlatformRoles.TestingOnlySuperUser.Name).Value,
            Role.Create(PlatformRoles.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenCreateWithListContainingInvalidItem_ThenReturnsError()
    {
        var result = Roles.Create(PlatformRoles.Standard.Name, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingValidItems_ThenReturnsValue()
    {
        var result = Roles.Create(PlatformRoles.Standard.Name, PlatformRoles.TestingOnly.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard.Name).Value,
            Role.Create(PlatformRoles.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenAddStringWithUnknownName_ThenAddsRole()
    {
        var roles = Roles.Create();

        var result = roles.Add("anunknownrole");

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(role => role.Identifier == "anunknownrole");
    }

    [Fact]
    public void WhenAddStringWithKnownName_ThenAddsRole()
    {
        var roles = Roles.Create();

        var result = roles.Add(PlatformRoles.Standard.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(role => role.Identifier == PlatformRoles.Standard.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddRoleLevel_ThenReturnsValue()
    {
        var roles = Roles.Create(PlatformRoles.Standard).Value;

        var result = roles.Add(PlatformRoles.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(
            Role.Create(PlatformRoles.Standard.Name).Value,
            Role.Create(PlatformRoles.TestingOnlySuperUser.Name).Value,
            Role.Create(PlatformRoles.TestingOnly.Name).Value);
    }
#endif
    [Fact]
    public void WhenAddRoleAndExists_ThenDoesNotAdd()
    {
        var roles = Roles.Create();
        roles.Add(PlatformRoles.Standard.Name);

        var result = roles.Add(PlatformRoles.Standard.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(role => role.Identifier == PlatformRoles.Standard.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddRoleAndNotExists_ThenAdds()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.Add(PlatformRoles.TestingOnly.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard.Name).Value,
            Role.Create(PlatformRoles.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var roles = Roles.Create();

        var result = roles.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var roles = Roles.Create();

        var result = roles.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasRoleAndInvalidName_ThenReturnsFalse()
    {
        var roles = Roles.Create();

        var result = roles.HasRole("anunknownrole");

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasRoleAndNoMatch_ThenReturnsFalse()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.HasRole(PlatformRoles.TestingOnly.Name);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasRoleAndMatching_ThenReturnsTrue()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.HasRole(PlatformRoles.Standard.Name);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.Remove("anunknownrole");

        result.Should().Be(roles);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.Remove(PlatformRoles.TestingOnly.Name);

        result.Should().Be(roles);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;

        var result = roles.Remove(PlatformRoles.Standard.Name);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenToList_ThenReturnsStringList()
    {
        var roles = Roles.Create();
        roles = roles.Add(PlatformRoles.Standard.Name).Value;
        roles = roles.Add(PlatformRoles.TestingOnly.Name).Value;

        var result = roles.ToList();

        result.Should().ContainInOrder(PlatformRoles.Standard.Name, PlatformRoles.TestingOnly.Name);
    }
#endif
}