using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class InvitationsApplicationSpec
{
    private const string TestingToken = "Ll4qhv77XhiXSqsTUc6icu56ZLrqu5p1gH9kT5IlHio";
    private readonly InvitationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IUserNotificationsService> _notificationsService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IInvitationRepository> _repository;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public InvitationsApplicationSpec()
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
            .Returns(TestingToken);

        _application =
            new InvitationsApplication(_recorder.Object, idFactory.Object, _tokensService.Object,
                _notificationsService.Object, _userProfilesService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenInviteGuestAsyncAndInviteeAlreadyRegistered_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        invitee.Register(Roles.Empty, Features.Empty, EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("aninvitee@company.com").Value);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee.ToOptional());
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

        var result =
            await _application.InviteGuestAsync(_caller.Object, "aninvitee@company.com", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.EndUsersApplication_GuestAlreadyRegistered);
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenInviteGuestAsyncAndEmailOwnerAlreadyRegistered_ThenDoesNothing()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
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

        var result =
            await _application.InviteGuestAsync(_caller.Object, "aninvitee@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.EmailAddress.Should().Be("aninvitee@company.com");
        result.Value.FirstName.Should().Be("afirstname");
        result.Value.LastName.Should().Be("alastname");
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 0
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
    public async Task WhenInviteGuestAsyncAndAlreadyInvited_ThenReInvitesGuest()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        await invitee.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            EmailAddress.Create("aninvitee@company.com").Value, (_, _) => Task.FromResult(Result.Ok));
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee.ToOptional());
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

        var result =
            await _application.InviteGuestAsync(_caller.Object, "aninvitee@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 0
            && eu.GuestInvitation.IsInvited
            && eu.GuestInvitation.InvitedById! == "aninviterid".ToId()
        ), It.IsAny<CancellationToken>()));
        result.Value.EmailAddress.Should().Be("aninvitee@company.com");
        result.Value.FirstName.Should().Be("Aninvitee");
        result.Value.LastName.Should().BeNull();
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(_caller.Object, TestingToken,
            "aninvitee@company.com", "Aninvitee", "aninviterdisplayname", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenInviteGuestAsync_ThenInvitesGuest()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
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

        var result =
            await _application.InviteGuestAsync(_caller.Object, "aninvitee@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.EmailAddress.Should().Be("aninvitee@company.com");
        result.Value.FirstName.Should().Be("Aninvitee");
        result.Value.LastName.Should().BeNull();
        _repository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships.Count == 0
            && eu.GuestInvitation.IsInvited
            && eu.GuestInvitation.InvitedById! == "aninviterid".ToId()
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.FindPersonByEmailAddressPrivateAsync(_caller.Object, "aninvitee@company.com",
                It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(_caller.Object, TestingToken,
            "aninvitee@company.com", "Aninvitee", "aninviterdisplayname", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("anid".ToId(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenResendGuestInvitationAsyncAndInvitationNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EndUserRoot>.None);

        var result =
            await _application.ResendGuestInvitationAsync(_caller.Object, TestingToken, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenResendGuestInvitationAsyncAndInvitationExists_ThenReInvites()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        await invitee.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            EmailAddress.Create("aninvitee@company.com").Value, (_, _) => Task.FromResult(Result.Ok));
        _repository.Setup(rep =>
                rep.FindInvitedGuestByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee.ToOptional());
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

        var result =
            await _application.ResendGuestInvitationAsync(_caller.Object, TestingToken, CancellationToken.None);

        result.Should().BeSuccess();
        _notificationsService.Verify(ns => ns.NotifyGuestInvitationToPlatformAsync(_caller.Object, TestingToken,
            "aninvitee@company.com", "Aninvitee", "aninviterdisplayname", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.LoadAsync("anid".ToId(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyGuestInvitationAsyncAndInvitationNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EndUserRoot>.None);

        var result =
            await _application.VerifyGuestInvitationAsync(_caller.Object, TestingToken, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenVerifyGuestInvitationAsyncAndInvitationExists_ThenVerifies()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("aninviterid");
        var inviter = EndUserRoot
            .Create(_recorder.Object, "aninviterid".ToIdentifierFactory(), UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync("aninviterid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        var invitee = EndUserRoot
            .Create(_recorder.Object, "aninviteeid".ToIdentifierFactory(), UserClassification.Person).Value;
        await invitee.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
            EmailAddress.Create("aninvitee@company.com").Value, (_, _) => Task.FromResult(Result.Ok));
        _repository.Setup(rep =>
                rep.FindInvitedGuestByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee.ToOptional());

        var result =
            await _application.VerifyGuestInvitationAsync(_caller.Object, TestingToken, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.EmailAddress.Should().Be("aninvitee@company.com");
        result.Value.FirstName.Should().Be("Aninvitee");
        result.Value.LastName.Should().BeNull();
    }
}