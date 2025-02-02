using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Subscriptions;
using EndUsersApplication.Persistence;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using Moq;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using Events = OrganizationsDomain.Events;
using Membership = EndUsersDomain.Membership;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationDomainEventHandlersSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUserRepository> _endUserRepository;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionsService> _subscriptionsService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public EndUsersApplicationDomainEventHandlersSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        var membershipCounter = 0;
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity entity) =>
            {
                if (entity is Membership)
                {
                    return $"amembershipid{membershipCounter++}".ToId();
                }

                return "anid".ToId();
            });
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(
                s => s.Platform.GetString(EndUsersApplication.PermittedOperatorsSettingName, It.IsAny<string?>()))
            .Returns("");
        _endUserRepository = new Mock<IEndUserRepository>();
        _endUserRepository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        _endUserRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, bool _, CancellationToken _) =>
                Task.FromResult<Result<EndUserRoot, Error>>(root));
        var invitationRepository = new Mock<IInvitationRepository>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _subscriptionsService = new Mock<ISubscriptionsService>();

        _application =
            new EndUsersApplication(_recorder.Object, _idFactory.Object, settings.Object,
                _userProfilesService.Object, _subscriptionsService.Object, invitationRepository.Object,
                _endUserRepository.Object);
    }

    [Fact]
    public async Task WhenHandleOrganizationCreatedAsyncAndUserNoExist_ThenReturnsError()
    {
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());
        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task HandleOrganizationCreatedAsyncForPerson_ThenAddsMembership()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        user.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, EmailAddress.Create("auser@company.com").Value);
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                }
            });
        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 1
            && eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
        ), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleOrganizationCreatedAsyncForAnInvitedMachine_ThenAddsMemberships()
    {
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        var machine = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Machine).Value;
        machine.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, EmailAddress.Create("auser@company.com").Value);
        machine.AddMembership(machine, OrganizationOwnership.Shared, "anorganizationid2".ToId(),
            Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(machine);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                }
            });
        var domainEvent = Events.Created("anorganizationid1".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 2
            && eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid2".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
            && !eu.Memberships[1].IsDefault
            && eu.Memberships[1].OrganizationId == "anorganizationid1".ToId()
            && eu.Memberships[1].Roles == Roles.Create(TenantRoles.BillingAdmin).Value
            && eu.Memberships[1].Features == Features.Create(TenantFeatures.PaidTrial).Value
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "anid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationRoleAssignedAsync_ThenAssigns()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Empty,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        assigner.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _endUserRepository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigner);
        var assignee = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assignee.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        assignee.AddMembership(assignee, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _endUserRepository.Setup(rep => rep.LoadAsync("anassigneeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        var domainEvent = Events.RoleAssigned("anorganizationid".ToId(), "anassignerid".ToId(), "anassigneeid".ToId(),
            Role.Create(TenantRoles.TestingOnly).Value);

        var result =
            await _application.HandleOrganizationRoleAssignedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Roles.HasRole(TenantRoles.TestingOnly)
        ), It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "anid",
            Audits.EndUsersApplication_TenantRolesAssigned, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationRoleUnassignedAsync_ThenAssigns()
    {
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Empty,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        assigner.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _endUserRepository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigner);
        var assignee = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assignee.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        assignee.AddMembership(assignee, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);
        assignee.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);
        _endUserRepository.Setup(rep => rep.LoadAsync("anassigneeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        var domainEvent = Events.RoleUnassigned("anorganizationid".ToId(), "anassignerid".ToId(), "anassigneeid".ToId(),
            Role.Create(TenantRoles.TestingOnly).Value);

        var result =
            await _application.HandleOrganizationRoleUnassignedAsync(_caller.Object, domainEvent,
                CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && !eu.Memberships[0].Roles.HasRole(TenantRoles.TestingOnly)
        ), It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "anid",
            Audits.EndUsersApplication_TenantRolesUnassigned, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationDeletedAsync_ThenRemovesMembership()
    {
        var deleter = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        deleter.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(TenantFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        deleter.AddMembership(deleter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);
        _endUserRepository.Setup(rep => rep.LoadAsync("adeleterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleter);
        var domainEvent = Events.Deleted("anorganizationid".ToId(), "adeleterid".ToId());

        var result =
            await _application.HandleOrganizationDeletedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 0
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleSubscriptionPlanChangedAsync_ThenReconcilesMemberships()
    {
        _caller.Setup(c => c.CallerId)
            .Returns(CallerConstants.MaintenanceAccountUserId);
        _subscriptionsService.Setup(ss =>
                ss.GetSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionWithPlan
            {
                Invoice = new InvoiceSummary
                {
                    Currency = "acurrency"
                },
                PaymentMethod = new SubscriptionPaymentMethod(),
                Period = new PlanPeriod(),
                Plan = new SubscriptionPlan
                {
                    Id = "aplanid"
                },
                SubscriptionReference = "asubscriptionreference",
                BuyerReference = "abuyerreference",
                BuyerId = "abuyerid",
                OwningEntityId = "anowningentityid",
                Id = "asubscriptionid"
            });
        _endUserRepository.Setup(rep => rep.SearchAllMembershipsByOrganizationAsync(It.IsAny<Identifier>(),
                It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipJoinInvitation>
            {
                new()
                {
                    UserId = "auserid"
                }
            });
        var member = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        member.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, Optional<EmailAddress>.None);
        member.AddMembership(member, OrganizationOwnership.Shared, "anowningentityid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);
        var domainEvent = SubscriptionsDomain.Events.SubscriptionPlanChanged("asubscriptionid".ToId(),
            "anowningentityid".ToId(), "aplanid".ToId(),
            BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } }).Value,
            "abuyerreference", "asubscriptionreference");

        var result =
            await _application.HandleSubscriptionPlanChangedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsService.Verify(ss =>
            ss.GetSubscriptionAsync(_caller.Object, "asubscriptionid".ToId(), It.IsAny<CancellationToken>()));
        _endUserRepository.Verify(rep => rep.SearchAllMembershipsByOrganizationAsync("anowningentityid".ToId(),
            It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
        _endUserRepository.Verify(eur => eur.SaveAsync(It.Is<EndUserRoot>(root =>
            root.Id == "anid".ToId()
        ), It.IsAny<CancellationToken>()));
    }
}