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
        var result = Role.Create(new RoleLevel(string.Empty));

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = Role.Create(new RoleLevel("^aninvalidname^"));

        result.Should().BeError(ErrorCode.Validation, Resources.Roles_InvalidRole);
    }

    [Fact]
    public void WhenCreateWithUnknownName_ThenReturnsValue()
    {
        var result = Role.Create(new RoleLevel("anunknownrole"));

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be("anunknownrole");
    }

    [Fact]
    public void WhenCreateWithKnownName_ThenReturnsValue()
    {
        var result = Role.Create(PlatformRoles.Standard);

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
        var result = Roles.Empty;

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
        var result = Roles.Create(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithRoleLevel_ThenReturnsValue()
    {
        var result = Roles.Create(PlatformRoles.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(rol => rol == Role.Create(PlatformRoles.TestingOnlySuperUser).Value);
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
        var result = Roles.Create(PlatformRoles.Standard, PlatformRoles.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value,
            Role.Create(PlatformRoles.TestingOnly).Value);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingParent_ThenReturnsNormalizedValue()
    {
        var result = Roles.Create(PlatformRoles.Operations, PlatformRoles.Standard, PlatformRoles.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Operations).Value,
            Role.Create(PlatformRoles.TestingOnly).Value);
    }
#endif

    [Fact]
    public void WhenAddStringWithUnknownName_ThenAddsRole()
    {
        var roles = Roles.Empty;

        var result = roles.Add("anunknownrole");

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(role => role.Identifier == "anunknownrole");
    }

    [Fact]
    public void WhenAddStringWithKnownName_ThenAddsRole()
    {
        var roles = Roles.Empty;

        var result = roles.Add(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(role => role.Identifier == PlatformRoles.Standard.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddRoleLevel_ThenReturnsValue()
    {
        var roles = Roles.Create(PlatformRoles.Standard).Value;

        var result = roles.Add(PlatformRoles.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(
            Role.Create(PlatformRoles.Standard).Value,
            Role.Create(PlatformRoles.TestingOnlySuperUser).Value);
    }
#endif

    [Fact]
    public void WhenAddParentRoleLevel_ThenReturnsNormalizedValue()
    {
        var roles = Roles.Create(PlatformRoles.Standard).Value;

        var result = roles.Add(PlatformRoles.Operations);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items.Should().ContainInOrder(
            Role.Create(PlatformRoles.Operations).Value);
    }

    [Fact]
    public void WhenAddChildRoleLevel_ThenReturnsNormalizedValue()
    {
        var roles = Roles.Create(PlatformRoles.Operations).Value;

        var result = roles.Add(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items.Should().ContainInOrder(
            Role.Create(PlatformRoles.Operations).Value);
    }

    [Fact]
    public void WhenAddRoleAndExists_ThenDoesNotAdd()
    {
        var roles = Roles.Empty;
        roles.Add(PlatformRoles.Standard);

        var result = roles.Add(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(role => role.Identifier == PlatformRoles.Standard.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddRoleAndNotExists_ThenAdds()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Add(PlatformRoles.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value,
            Role.Create(PlatformRoles.TestingOnly).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var roles = Roles.Empty;

        var result = roles.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var roles = Roles.Empty;

        var result = roles.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasRoleAndInvalidName_ThenReturnsFalse()
    {
        var roles = Roles.Empty;

        var result = roles.HasRole(new RoleLevel("anunknownrole"));

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasRoleAndNoMatch_ThenReturnsFalse()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasRole(PlatformRoles.TestingOnly);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasRoleAndHasSameRole_ThenReturnsTrue()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Operations).Value;

        var result = roles.HasRole(PlatformRoles.Operations);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasRoleAndHasParentRole_ThenReturnsFalse()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasRole(PlatformRoles.Operations);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasRoleAndHasChildRole_ThenReturnsTrue()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Operations).Value;

        var result = roles.HasRole(PlatformRoles.Standard);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove("anunknownrole");

        result.Should().Be(roles);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove(PlatformRoles.TestingOnly);

        result.Should().Be(roles);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove(PlatformRoles.Standard);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenRemoveChildAndHasParent_ThenLeavesParent()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Operations).Value;

        var result = roles.Remove(PlatformRoles.Standard);

        result.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Operations).Value);
    }

    [Fact]
    public void WhenRemoveParentAndHasDescendants_ThenLeavesDescendants()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Operations).Value;

        var result = roles.Remove(PlatformRoles.Operations);

        result.Items.Count.Should().Be(1);
        result.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value);
    }

    [Fact]
    public void WhenRemoveParentAndNoDescendants_ThenRemovesParent()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove(PlatformRoles.Standard);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenDenormalize_ThenReturnsDenormalizedList()
    {
        var roles = Roles.Empty;
        roles = roles.Add(PlatformRoles.Operations).Value;
        roles = roles.Add(PlatformRoles.TestingOnlySuperUser).Value;

        var result = roles.Denormalize();

        result.Count.Should().Be(4);
        result.Should().ContainInOrder(PlatformRoles.Operations.Name, PlatformRoles.Standard.Name,
            PlatformRoles.TestingOnlySuperUser.Name, PlatformRoles.TestingOnly.Name);
    }
#endif
}