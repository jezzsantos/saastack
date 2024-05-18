using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using Moq;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using Events = OrganizationsDomain.Events;
using Membership = EndUsersDomain.Membership;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationDomainEventHandlersSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUserRepository> _endUserRepository;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

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
        var invitationRepository = new Mock<IInvitationRepository>();
        var userProfilesService = new Mock<IUserProfilesService>();
        var notificationsService = new Mock<INotificationsService>();

        _application =
            new EndUsersApplication(_recorder.Object, _idFactory.Object, settings.Object, notificationsService.Object,
                userProfilesService.Object, invitationRepository.Object, _endUserRepository.Object);
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
    public async Task HandleOrganizationCreatedAsync_ThenAddsMembership()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        user.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, EmailAddress.Create("auser@company.com").Value);
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
        ), It.IsAny<CancellationToken>()));
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
            Audits.EndUserApplication_TenantRolesAssigned, It.IsAny<string>(),
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
            Roles.Create(TenantRoles.TestingOnly).Value);
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
            Audits.EndUserApplication_TenantRolesUnassigned, It.IsAny<string>(),
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
}