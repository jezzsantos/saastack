using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;
using PersonName = Application.Resources.Shared.PersonName;
using Task = System.Threading.Tasks.Task;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class PasswordCredentialsApplicationMfaSpec
{
    private readonly PasswordCredentialsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IMfaService> _mfaService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPasswordCredentialsRepository> _repository;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserNotificationsService> _userNotificationsService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public PasswordCredentialsApplicationMfaSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        var authenticatorCounter = 0;
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity entity) =>
            {
                if (entity is MfaAuthenticator)
                {
                    return $"anauthenticatorid{++authenticatorCounter}".ToId();
                }

                return "anid".ToId();
            });
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _endUsersService = new Mock<IEndUsersService>();
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person
            });
        _userProfilesService = new Mock<IUserProfilesService>();
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                EmailAddress = "auser@company.com"
            });
        _userNotificationsService = new Mock<IUserNotificationsService>();
        _settings = new Mock<IConfigurationSettings>();
        _settings.Setup(s => s.Platform.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null!);
        _settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns(5);
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(true);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateRegistrationVerificationToken())
            .Returns("averificationtoken");
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns("apasswordhash");
        _passwordHasherService.Setup(phs => phs.ValidatePasswordHash(It.IsAny<string>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mfaService = new Mock<IMfaService>();
        _mfaService.Setup(ts => ts.GenerateOobCode())
            .Returns("anoobcode");
        var authTokensService = new Mock<IAuthTokensService>();
        var websiteUiService = new Mock<IWebsiteUiService>();
        _repository = new Mock<IPasswordCredentialsRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()))
            .Returns((PasswordCredentialRoot root, CancellationToken _) =>
                Task.FromResult<Result<PasswordCredentialRoot, Error>>(root));
        _repository.Setup(rep =>
                rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((PasswordCredentialRoot root, bool _, CancellationToken _) =>
                Task.FromResult<Result<PasswordCredentialRoot, Error>>(root));

        _application = new PasswordCredentialsApplication(_recorder.Object, _idFactory.Object, _endUsersService.Object,
            _userProfilesService.Object, _userNotificationsService.Object, _settings.Object,
            _emailAddressService.Object,
            _tokensService.Object, _passwordHasherService.Object, _mfaService.Object, authTokensService.Object,
            websiteUiService.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenChangeMfaAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.ChangeMfaAsync(_caller.Object, true, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeMfaAsyncAndNotAPerson_ThenReturnsError()
    {
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Machine
            });

        var result =
            await _application.ChangeMfaAsync(_caller.Object, true, CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PasswordCredentialsApplication_NotPerson);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeMfaAsync_ThenEnablesMfa()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person
            });

        var result =
            await _application.ChangeMfaAsync(_caller.Object, true, CancellationToken.None);

        result.Should().BeSuccess();
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(root =>
            root.MfaOptions.IsEnabled
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialsByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, "anmfatoken",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsyncByAuthenticatedUserButNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, "anmfatoken",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsyncByAnonymous_ThenReturnsAuthenticators()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, "anmfatoken",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsyncByAuthenticatedUser_ThenReturnsAuthenticators()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.DisassociateMfaAuthenticatorAsync(_caller.Object, "anauthenticatorid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAsync_ThenDeletes()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        var authenticator = await credential.AssociateMfaAuthenticatorAsync("auserid".ToId(),
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.DisassociateMfaAuthenticatorAsync(_caller.Object, authenticator.Value.Id,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(cred =>
            cred.MfaAuthenticators.HasNone()
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, null,
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialsByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAuthenticatedUserButNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>.None);

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAnonymousForTotpAndNotAPerson_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Machine
            });

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PasswordCredentialsApplication_NotPerson);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAnonymousForTotp_ThenAssociates()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(PasswordCredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().BeNull();
        result.Value.BarCodeUri.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.TotpAuthenticator
            && cred.MfaAuthenticators[1].OobCode == Optional<string>.None
            && cred.MfaAuthenticators[1].BarCodeUri == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAuthenticatedUserForTotp_ThenAssociates()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, null,
                PasswordCredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(PasswordCredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().BeNull();
        result.Value.BarCodeUri.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.TotpAuthenticator
            && cred.MfaAuthenticators[1].OobCode == Optional<string>.None
            && cred.MfaAuthenticators[1].BarCodeUri == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAuthenticatedUserForOobSms_ThenAssociates()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, null,
                PasswordCredentialMfaAuthenticatorType.OobSms, "+6498876986",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(PasswordCredentialMfaAuthenticatorType.OobSms);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().Be("anoobcode");
        result.Value.BarCodeUri.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.OobSms
            && cred.MfaAuthenticators[1].OobCode == "anoobcode"
            && cred.MfaAuthenticators[1].BarCodeUri == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
        _userNotificationsService.Verify(ns =>
            ns.NotifyPasswordMfaOobSmsAsync(_caller.Object, "+6498876986",
                "anoobcode", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsyncByAuthenticatedUserForOobEmail_ThenAssociates()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, null,
                PasswordCredentialMfaAuthenticatorType.OobEmail, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(PasswordCredentialMfaAuthenticatorType.OobEmail);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().Be("anoobcode");
        result.Value.BarCodeUri.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.OobEmail
            && cred.MfaAuthenticators[1].OobCode == "anoobcode"
            && cred.MfaAuthenticators[1].BarCodeUri == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
        _userNotificationsService.Verify(ns =>
            ns.NotifyPasswordMfaOobEmailAsync(_caller.Object, "auser@company.com",
                "anoobcode", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()));
    }

    private PasswordCredentialRoot CreateUnVerifiedCredential()
    {
        var credential = CreateCredential();
        credential.SetPasswordCredential("apassword");
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();

        return credential;
    }

    private PasswordCredentialRoot CreateVerifiedCredential()
    {
        var credential = CreateUnVerifiedCredential();
        credential.VerifyRegistration();
        return credential;
    }

    private PasswordCredentialRoot CreateCredential()
    {
        return PasswordCredentialRoot.Create(_recorder.Object, _idFactory.Object, _settings.Object,
            _emailAddressService.Object, _tokensService.Object, _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId()).Value;
    }
}