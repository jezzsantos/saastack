using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared.DomainServices;
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
    private readonly Mock<ITokensService> _tokensService;
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
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateGuestInvitationToken())
            .Returns("aninvitationtoken");

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
        _user.GuestInvitation.IsInvited.Should().BeFalse();
    }

    [Fact]
    public async Task WhenRegisterAndInvitedAsGuest_ThenAcceptsInvitationAndRegistered()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value, emailAddress);

        _user.Access.Should().Be(UserAccess.Enabled);
        _user.Status.Should().Be(UserStatus.Registered);
        _user.Classification.Should().Be(UserClassification.Person);
        _user.Roles.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard.Name).Value);
        _user.Features.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic.Name).Value);
        _user.GuestInvitation.IsAccepted.Should().BeTrue();
        _user.GuestInvitation.AcceptedEmailAddress.Should().Be(emailAddress);
        _user.Events[2].Should().BeOfType<Events.GuestInvitationAccepted>();
        _user.Events.Last().Should().BeOfType<Events.Registered>();
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
    public void WhenEnsureInvariantsAndRegisteredPersonStillInvited_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            emailAddress);
#if TESTINGONLY
        _user.TestingOnly_InviteGuest(emailAddress);
#endif

        var result = _user.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestAlreadyRegistered);
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
    public void WhenAddMembership_ThenAddsMembershipAsDefaultWithRolesAndFeatures()
    {
        _user.Register(Roles.Create(PlatformRoles.Standard.Name).Value,
            Features.Create(PlatformFeatures.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);
        var roles = Roles.Create(TenantRoles.Member).Value;
        var features = Features.Create(TenantFeatures.Basic).Value;

        var result = _user.AddMembership("anorganizationid".ToId(), roles, features);

        result.Should().BeSuccess();
        _user.Memberships.Should().ContainSingle(ms =>
            ms.OrganizationId.Value == "anorganizationid"
            && ms.IsDefault
            && ms.Roles == roles
            && ms.Features == features);
        _user.Events.Last().Should().BeOfType<Events.MembershipAdded>();
    }

    [Fact]
    public void WhenAddMembershipAndHasMembership_ThenChangesNextToDefaultMembership()
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
            ms.OrganizationId.Value == "anorganizationid1"
            && !ms.IsDefault
            && ms.Roles == roles
            && ms.Features == features);
        _user.Memberships.Should().Contain(ms =>
            ms.OrganizationId.Value == "anorganizationid2"
            && ms.IsDefault
            && ms.Roles == roles
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

#if TESTINGONLY
    [Fact]
    public void WhenUnassignPlatformRolesAndAssignerNotOperator_ThenReturnsError()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person).Value;

        var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOperator);
    }
#endif

    [Fact]
    public void WhenUnassignPlatformRolesAndRoleNotAssignable_ThenReturnsError()
    {
        var assigner = CreateOperator();

        var result = _user.UnassignPlatformRoles(assigner, Roles.Create("anunknownrole").Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_UnassignablePlatformRole.Format("anunknownrole"));
    }

#if TESTINGONLY
    [Fact]
    public void WhenUnassignPlatformRolesAndUserNotAssignedRole_ThenReturnsError()
    {
        var assigner = CreateOperator();

        var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_CannotUnassignUnassignedRole.Format(PlatformRoles.TestingOnly.Name));
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenUnassignPlatformRolesAndStandardRole_ThenReturnsError()
    {
        var assigner = CreateOperator();

        var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.Standard).Value);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EndUserRoot_CannotUnassignBaselinePlatformRole.Format(PlatformRoles.Standard.Name));
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenUnassignPlatformRoles_ThenUnassigns()
    {
        var assigner = CreateOperator();
        _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value);

        result.Should().BeSuccess();
        _user.Roles.HasNone().Should().BeTrue();
        _user.Features.HasNone().Should().BeTrue();
        _user.Events.Last().Should().BeOfType<Events.PlatformRoleUnassigned>();
    }
#endif

    [Fact]
    public async Task WhenInviteAsGuestAndRegistered_ThenDoesNothing()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        _user.Register(Roles.Empty, Features.Empty, emailAddress);
        var wasCallbackCalled = false;

        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeFalse();
        _user.Events.Last().Should().BeOfType<Events.Created>();
        _user.GuestInvitation.IsInvited.Should().BeFalse();
        _user.GuestInvitation.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task WhenInviteAsGuestAndAlreadyInvited_ThenInvitedAgain()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
        var wasCallbackCalled = false;

        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeTrue();
        _user.Events.Last().Should().BeOfType<Events.GuestInvitationCreated>();
        _user.GuestInvitation.IsInvited.Should().BeTrue();
        _user.GuestInvitation.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task WhenInviteAsGuestAndUnknown_ThenInvited()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        var wasCallbackCalled = false;

        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeTrue();
        _user.Events.Last().Should().BeOfType<Events.GuestInvitationCreated>();
        _user.GuestInvitation.IsInvited.Should().BeTrue();
        _user.GuestInvitation.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReInviteGuestAsyncAndNotInvited_ThenReturnsError()
    {
        var wasCallbackCalled = false;

        var result = await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeFalse();
        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
    }

    [Fact]
    public async Task WhenReInviteGuestAsyncAndInvitationExpired_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
        _user.TestingOnly_ExpireGuestInvitation();
#endif
        var wasCallbackCalled = false;

        var result = await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeFalse();
        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
    }

    [Fact]
    public async Task WhenReInviteGuestAsyncAndInvited_ThenReInvites()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
        var wasCallbackCalled = false;

        await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            (_, _) =>
            {
                wasCallbackCalled = true;
                return Task.FromResult(Result.Ok);
            });

        wasCallbackCalled.Should().BeTrue();
        _user.Events.Last().Should().BeOfType<Events.GuestInvitationCreated>();
        _user.GuestInvitation.IsInvited.Should().BeTrue();
        _user.GuestInvitation.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyGuestInvitationAndAlreadyRegistered_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        _user.Register(Roles.Empty, Features.Empty, emailAddress);

        var result = _user.VerifyGuestInvitation();

        result.Should().BeError(ErrorCode.EntityExists, Resources.EndUserRoot_GuestAlreadyRegistered);
    }

    [Fact]
    public void WhenVerifyGuestInvitationAndNotInvited_ThenReturnsError()
    {
        var result = _user.VerifyGuestInvitation();

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
    }

    [Fact]
    public async Task WhenVerifyGuestInvitationAndInvitationExpired_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
        _user.TestingOnly_ExpireGuestInvitation();
#endif

        var result = _user.VerifyGuestInvitation();

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
    }

    [Fact]
    public async Task WhenVerifyGuestInvitationAndStillValid_ThenVerifies()
    {
        var emailAddress = EmailAddress.Create("invitee@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));

        var result = _user.VerifyGuestInvitation();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenAcceptGuestInvitationAndAuthenticatedUser_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = _user.AcceptGuestInvitation("auserid".ToId(), emailAddress);

        result.Should().BeError(ErrorCode.ForbiddenAccess,
            Resources.EndUserRoot_GuestInvitationAcceptedByNonAnonymousUser);
    }

    [Fact]
    public void WhenAcceptGuestInvitationAndRegistered_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
        _user.Register(Roles.Empty, Features.Empty, emailAddress);

        var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

        result.Should().BeError(ErrorCode.EntityExists, Resources.EndUserRoot_GuestAlreadyRegistered);
    }

    [Fact]
    public void WhenAcceptGuestInvitationAndNotInvited_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
    }

    [Fact]
    public async Task WhenAcceptGuestInvitationAndInviteExpired_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
        _user.TestingOnly_ExpireGuestInvitation();
#endif

        var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
    }

    [Fact]
    public async Task WhenAcceptGuestInvitation_ThenAccepts()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
        await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
            (_, _) => Task.FromResult(Result.Ok));

        var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

        result.Should().BeSuccess();
        _user.Events.Last().Should().BeOfType<Events.GuestInvitationAccepted>();
        _user.GuestInvitation.IsAccepted.Should().BeTrue();
        _user.GuestInvitation.AcceptedEmailAddress.Should().Be(emailAddress);
    }

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