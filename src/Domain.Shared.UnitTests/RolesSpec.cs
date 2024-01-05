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
    public void WhenCreateWithUnknownName_ThenReturnsError()
    {
        var result = Role.Create("anunknownrole");

        result.Should().BeError(ErrorCode.Validation, Resources.Roles_InvalidRole);
    }

    [Fact]
    public void WhenCreateWithKnownName_ThenReturnsValue()
    {
        var result = Role.Create(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be(PlatformRoles.Standard);
    }
}

[Trait("Category", "Unit")]
public class RolesSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsError()
    {
        var result = Roles.Create();

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
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

    [Fact]
    public void WhenCreateWithEmptyList_ThenReturnsValue()
    {
        var result = Roles.Create(Enumerable.Empty<string>());

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithListContainingInvalidItem_ThenReturnsError()
    {
        var result = Roles.Create(new[] { PlatformRoles.Standard, string.Empty });

        result.Should().BeError(ErrorCode.Validation);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingValidItems_ThenReturnsValue()
    {
        var result = Roles.Create(new[] { PlatformRoles.Standard, PlatformRoles.TestingOnlyUser });

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value,
            Role.Create(PlatformRoles.TestingOnlyUser).Value);
    }
#endif

    [Fact]
    public void WhenAddStringAndInvalid_ThenReturnsError()
    {
        var roles = Roles.Create().Value;

        var result = roles.Add("anunknownrole");

        result.Should().BeError(ErrorCode.Validation, Resources.Roles_InvalidRole);
    }

    [Fact]
    public void WhenAddStringValid_ThenAddsRole()
    {
        var roles = Roles.Create().Value;

        var result = roles.Add(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(role => role.Identifier == PlatformRoles.Standard);
    }

    [Fact]
    public void WhenAddRoleAndExists_ThenDoesNotAdd()
    {
        var roles = Roles.Create().Value;
        roles.Add(PlatformRoles.Standard);

        var result = roles.Add(PlatformRoles.Standard);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(role => role.Identifier == PlatformRoles.Standard);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddRoleAndNotExists_ThenAdds()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Add(PlatformRoles.TestingOnlyUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value,
            Role.Create(PlatformRoles.TestingOnlyUser).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var roles = Roles.Create().Value;

        var result = roles.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var roles = Roles.Create().Value;

        var result = roles.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasRoleAndInvalidName_ThenReturnsFalse()
    {
        var roles = Roles.Create().Value;

        var result = roles.HasRole("anunknownrole");

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasRoleAndNoMatch_ThenReturnsFalse()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasRole(PlatformRoles.TestingOnlyUser);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasRoleAndMatching_ThenReturnsTrue()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.HasRole(PlatformRoles.Standard);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove("anunknownrole");

        result.Should().Be(roles);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove(PlatformRoles.TestingOnlyUser);

        result.Should().Be(roles);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;

        var result = roles.Remove(PlatformRoles.Standard);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenToList_ThenReturnsStringList()
    {
        var roles = Roles.Create().Value;
        roles = roles.Add(PlatformRoles.Standard).Value;
        roles = roles.Add(PlatformRoles.TestingOnlyUser).Value;

        var result = roles.ToList();

        result.Should().ContainInOrder(PlatformRoles.Standard, PlatformRoles.TestingOnlyUser);
    }
#endif
}