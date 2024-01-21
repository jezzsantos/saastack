using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EndUsersDomain.UnitTests;

[Trait("Category", "Unit")]
public class EndUserRootSpec
{
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly EndUserRoot _user;

    public EndUserRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        var counter = 0;
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity entity) =>
            {
                if (entity is Membership)
                {
                    return $"amembershipid{++counter}".ToId();
                }

                return "anid".ToId();
            });
        _user = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _user.Access.Should().Be(UserAccess.Enabled);
        _user.Status.Should().Be(UserStatus.Unregistered);
        _user.Classification.Should().Be(UserClassification.Person);
        _user.Roles.HasNone().Should().BeTrue();
        _user.Features.HasNone().Should().BeTrue();
    }

    [Fact]
    public void WhenRegister_ThenRegistered()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);

        _user.Access.Should().Be(UserAccess.Enabled);
        _user.Status.Should().Be(UserStatus.Registered);
        _user.Classification.Should().Be(UserClassification.Person);
        _user.Roles.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard.Name).Value);
        _user.Features.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic.Name).Value);
        _user.Events.Last().Should().BeOfType<Events.Registered>();
    }

    [Fact]
    public void WhenEnsureInvariantsAndMachineIsNotRegistered_ThenReturnsError()
    {
        var machine = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Machine).Value;

        var result = machine.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_MachineNotRegistered);
    }

    [Fact]
    public void WhenEnsureInvariantsAndRegisteredPersonDoesNotHaveADefaultRole_ThenReturnsError()
    {
        _user.Register(Roles.Create(),
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);

        var result = _user.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_AllPersonsMustHaveDefaultRole);
    }

    [Fact]
    public void WhenEnsureInvariantsAndRegisteredPersonDoesNotHaveADefaultFeature_ThenReturnsError()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(),
            EmailAddress.Create("auser@company.com").Value);

        var result = _user.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_AllPersonsMustHaveDefaultFeature);
    }

    [Fact]
    public void WhenAddMembershipAndNotRegistered_ThenReturnsError()
    {
        var result = _user.AddMembership("anorganizationid".ToId(), Roles.Create(), Features.Create());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotRegistered);
    }

    [Fact]
    public void WhenAddMembershipAndAlreadyMember_ThenReturns()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        _user.AddMembership("anorganizationid".ToId(), Roles.Create(), Features.Create());

        var result = _user.AddMembership("anorganizationid".ToId(), Roles.Create(), Features.Create());

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenAddMembership_ThenAddsMembershipRolesAndFeatures()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        var roles = Roles.Create(TenantRoles.Member).Value;
        var features = Features.Create(TenantFeatures.Basic).Value;

        var result = _user.AddMembership("anorganizationid".ToId(), roles, features);

        result.Should().BeSuccess();
        _user.Memberships.Should().ContainSingle(ms =>
            ms.OrganizationId.Value == "anorganizationid" && ms.IsDefault && ms.Roles == roles
            && ms.Features == features);
        _user.Events.Last().Should().BeOfType<Events.MembershipAdded>();
    }

    [Fact]
    public void WhenAddMembershipAndNextMembership_ThenChangesNextToDefaultMembership()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        var roles = Roles.Create(TenantRoles.Member).Value;
        var features = Features.Create(TenantFeatures.Basic).Value;
        _user.AddMembership("anorganizationid1".ToId(), roles, features);

        var result = _user.AddMembership("anorganizationid2".ToId(), roles, features);

        result.Should().BeSuccess();
        _user.Memberships.Should().Contain(ms =>
            ms.OrganizationId.Value == "anorganizationid1" && !ms.IsDefault && ms.Roles == roles
            && ms.Features == features);
        _user.Memberships.Should().Contain(ms =>
            ms.OrganizationId.Value == "anorganizationid2" && ms.IsDefault && ms.Roles == roles
            && ms.Features == features);
        _user.Events.Last().Should().BeOfType<Events.MembershipDefaultChanged>();
    }

#if TESTINGONLY
    [Fact]
    public void WhenAssignMembershipFeaturesAndAssignerNotOwner_ThenReturnsError()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;

        var result = _user.AssignMembershipFeatures(assigner, "anorganizationid".ToId(),
            Features.Create(TenantFeatures.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOrganizationOwner);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAssignMembershipFeaturesAndNoMembership_ThenReturnsError()
    {
        var assigner = CreateOrgOwner("anorganizationid");

        var result = _user.AssignMembershipFeatures(assigner, "anorganizationid".ToId(),
            Features.Create(TenantFeatures.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NoMembership.Format("anorganizationid"));
    }
#endif

    [Fact]
    public void WhenAssignMembershipFeaturesAndFeatureNotAssignable_ThenReturnsError()
    {
        var assigner = CreateOrgOwner("anorganizationid");
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        _user.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);

        var result = _user.AssignMembershipFeatures(assigner, "anorganizationid".ToId(),
            Features.Create("anunknownfeature").Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_UnassignableTenantFeature.Format("anunknownfeature"));
    }

#if TESTINGONLY
    [Fact]
    public void WhenAssignMembershipFeatures_ThenAssigns()
    {
        var assigner = CreateOrgOwner("anorganizationid");
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        _user.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);

        var result = _user.AssignMembershipFeatures(assigner, "anorganizationid".ToId(),
            Features.Create(TenantFeatures.TestingOnly).Value);

        result.Should().BeSuccess();
        _user.Memberships[0].Roles.Should().Be(Roles.Create(TenantRoles.Member.Name).Value);
        _user.Memberships[0].Features.Should()
            .Be(Features.Create(TenantFeatures.Basic.Name, TenantFeatures.TestingOnly.Name).Value);
        _user.Events.Last().Should().BeOfType<Events.MembershipFeatureAssigned>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAssignMembershipRolesAndAssignerNotOwner_ThenReturnsError()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;

        var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOrganizationOwner);
    }
#endif

    [Fact]
    public void WhenAssignMembershipRolesAndRoleNotAssignable_ThenReturnsError()
    {
        var assigner = CreateOrgOwner("anorganizationid");
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        _user.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);

        var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
            Roles.Create("anunknownrole").Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_UnassignableTenantRole.Format("anunknownrole"));
    }

#if TESTINGONLY
    [Fact]
    public void WhenAssignMembershipRoles_ThenAssigns()
    {
        var assigner = CreateOrgOwner("anorganizationid");
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        _user.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);

        var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.TestingOnly).Value);

        result.Should().BeSuccess();
        _user.Memberships[0].Roles.Should()
            .Be(Roles.Create(TenantRoles.Member.Name, TenantRoles.TestingOnly.Name).Value);
        _user.Memberships[0].Features.Should()
            .Be(Features.Create(TenantFeatures.Basic.Name).Value);
        _user.Events.Last().Should().BeOfType<Events.MembershipRoleAssigned>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAssignPlatformFeaturesAndAssignerNotOperator_ThenReturnsError()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;

        var result = _user.AssignPlatformFeatures(assigner, Features.Create(PlatformFeatures.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOperator);
    }
#endif

    [Fact]
    public void WhenAssignPlatformFeaturesAndFeatureNotAssignable_ThenReturnsError()
    {
        var assigner = CreateOperator();

        var result = _user.AssignPlatformFeatures(assigner, Features.Create("anunknownfeature").Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_UnassignablePlatformFeature.Format("anunknownfeature"));
    }

#if TESTINGONLY
    [Fact]
    public void WhenAssignPlatformFeatures_ThenAssigns()
    {
        var assigner = CreateOperator();

        var result = _user.AssignPlatformFeatures(assigner, Features.Create(PlatformFeatures.TestingOnly).Value);

        result.Should().BeSuccess();
        _user.Roles.HasNone().Should().BeTrue();
        _user.Features.Should().Be(Features.Create(PlatformFeatures.TestingOnly.Name).Value);
        _user.Events.Last().Should().BeOfType<Events.PlatformFeatureAssigned>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAssignPlatformRolesAndAssignerNotOperator_ThenReturnsError()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;

        var result = _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOperator);
    }
#endif

    [Fact]
    public void WhenAssignPlatformRolesAndRoleNotAssignable_ThenReturnsError()
    {
        var assigner = CreateOperator();

        var result = _user.AssignPlatformRoles(assigner, Roles.Create("anunknownrole").Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_UnassignablePlatformRole.Format("anunknownrole"));
    }

#if TESTINGONLY
    [Fact]
    public void WhenAssignPlatformRoles_ThenAssigns()
    {
        var assigner = CreateOperator();

        var result = _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        result.Should().BeSuccess();
        _user.Roles.Should().Be(Roles.Create(PlatformRoles.TestingOnly.Name).Value);
        _user.Features.HasNone().Should().BeTrue();
        _user.Events.Last().Should().BeOfType<Events.PlatformRoleAssigned>();
    }
#endif

    private EndUserRoot CreateOrgOwner(string organizationId)
    {
        var owner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;
        owner.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("orgowner@company.com").Value);
        owner.AddMembership(organizationId.ToId(), Roles.Create(TenantRoles.Owner).Value, Features.Empty);

        return owner;
    }

    private EndUserRoot CreateOperator()
    {
        var @operator = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person)
            .Value;
        @operator.Register(Roles.Create(PlatformRoles.Standard.Name, PlatformRoles.Operations.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("operator@company.com").Value);

        return @operator;
    }
}