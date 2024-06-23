using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using Moq;
using UnitTesting.Common;
using Xunit;
using Events = OrganizationsDomain.Events;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class InvitationsApplicationDomainEventHandlersSpec
{
    private readonly InvitationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IUserNotificationsService> _notificationsService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IInvitationRepository> _repository;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public InvitationsApplicationDomainEventHandlersSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(
                s => s.Platform.GetString(EndUsersApplication.PermittedOperatorsSettingName, It.IsAny<string?>()))
            .Returns("");
        _repository = new Mock<IInvitationRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        _userProfilesService = new Mock<IUserProfilesService>();
        _notificationsService = new Mock<IUserNotificationsService>();
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateGuestInvitationToken())
            .Returns("aninvitationtoken");

        _application =
            new InvitationsApplication(_recorder.Object, idFactory.Object, _tokensService.Object,
                _notificationsService.Object, _userProfilesService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenHandleOrganizationMemberInvitedAsyncAndNoUserIdNorEmailAddress_ThenReturnsError()
    {
        var domainEvent = Events.MemberInvited("anorganizationid".ToId(), "aninviterid".ToId(),
            Optional<Identifier>.None,
            Optional<EmailAddress>.None);

        var result =
            await _application.HandleOrganizationMemberInvitedAsync(_caller.Object, domainEvent,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.InvitationsApplication_InviteMemberToOrganization_NoUserIdNorEmailAddress);
    }

    [Fact]
    public async Task WhenHandleOrganizationMemberInvitedAsyncWithRegisteredUserEmail_ThenAddsMembership()
    {
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        inviter.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Empty);
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EndUserRoot>.None);
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                EmailAddress = "aninvitee@company.com",
                UserId = "aninviteeid",
                Id = "aprofileid"
            }.ToOptional());
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviteeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee);
        var domainEvent = Events.MemberInvited("anorganizationid".ToId(), "aninviterid".ToId(),
            Optional<Identifier>.None,
            EmailAddress.Create("aninvitee@company.com").Value);

        var result =
            await _application.HandleOrganizationMemberInvitedAsync(_caller.Object, domainEvent,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 1
            && eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
            && eu.GuestInvitation.IsInvited == false
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.FindPersonByEmailAddressPrivateAsync(_caller.Object, "aninvitee@company.com",
                It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("aninviteeid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationMemberInvitedAsyncWithGuestEmailAddress_ThenAddsMembership()
    {
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        inviter.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Empty);
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EndUserRoot>.None);
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfile>.None);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), "aninviterid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "aninviterdisplayname",
                Name = new PersonName
                {
                    FirstName = "aninviterfirstname"
                },
                UserId = "aninviterid",
                Id = "aprofileid"
            });
        var domainEvent = Events.MemberInvited("anorganizationid".ToId(), "aninviterid".ToId(),
            Optional<Identifier>.None,
            EmailAddress.Create("aninvitee@company.com").Value);

        var result =
            await _application.HandleOrganizationMemberInvitedAsync(_caller.Object, domainEvent,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 1
            && eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
            && eu.GuestInvitation.IsInvited
            && eu.GuestInvitation.InvitedById! == "aninviterid".ToId()
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.FindPersonByEmailAddressPrivateAsync(_caller.Object, "aninvitee@company.com",
                It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(_caller.Object, "aninvitationtoken",
            "aninvitee@company.com", "Aninvitee", "aninviterdisplayname", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("anid".ToId(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationMemberInvitedAsyncWithUserId_ThenAddsMembership()
    {
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        inviter.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Empty);
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        await invitee.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            EmailAddress.Create("aninvitee@company.com").Value, (_, _) => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.LoadAsync("aninviteeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), "aninviterid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "aninviterdisplayname",
                Name = new PersonName
                {
                    FirstName = "aninviterfirstname"
                },
                UserId = "aninviterid",
                Id = "aprofileid"
            });
        var domainEvent = Events.MemberInvited("anorganizationid".ToId(), "aninviterid".ToId(), "aninviteeid".ToId(),
            Optional<EmailAddress>.None);

        var result =
            await _application.HandleOrganizationMemberInvitedAsync(_caller.Object, domainEvent,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 1
            && eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
            && eu.GuestInvitation.IsInvited
            && eu.GuestInvitation.InvitedById! == "aninviterid".ToId()
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(_caller.Object, "aninvitationtoken",
            "aninvitee@company.com", "Aninvitee", "aninviterdisplayname", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(_caller.Object, "aninviterid", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("aninviteeid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationMemberUnInvitedAsync_ThenRemovesMembership()
    {
        var inviter = EndUserRoot
            .Create(_recorder.Object, "anuninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        inviter.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Empty);
        _repository.Setup(rep => rep.LoadAsync("anuninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "anuninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        await invitee.InviteGuestAsync(_tokensService.Object, "anuninviterid".ToId(),
            EmailAddress.Create("aninvitee@company.com").Value, (_, _) => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.LoadAsync("anuninviteeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee);
        var domainEvent =
            Events.MemberUnInvited("anorganizationid".ToId(), "anuninviterid".ToId(), "anuninviteeid".ToId());

        var result = await _application.HandleOrganizationMemberUnInvitedAsync(_caller.Object, domainEvent,
            CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 0
        ), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("anuninviterid".ToId(), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("anuninviteeid".ToId(), It.IsAny<CancellationToken>()));
    }
}