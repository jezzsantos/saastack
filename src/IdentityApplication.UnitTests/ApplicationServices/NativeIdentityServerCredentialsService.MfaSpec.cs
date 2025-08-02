using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
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

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerCredentialsMfaServiceSpec
{
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IMfaService> _mfaService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPersonCredentialRepository> _repository;
    private readonly NativeIdentityServerCredentialsService _service;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserNotificationsService> _userNotificationsService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public NativeIdentityServerCredentialsMfaServiceSpec()
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
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
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
        _mfaService.Setup(ms => ms.GenerateOobCode())
            .Returns("anoobcode");
        _mfaService.Setup(ms => ms.GenerateOobSecret())
            .Returns("anoobsecret");
        _mfaService.Setup(ms => ms.GenerateOtpSecret())
            .Returns("anotpsecret");
        _mfaService.Setup(ms => ms.GenerateOtpBarcodeUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("abarcodeuri");
        _mfaService.Setup(ms => ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1));
        _mfaService.Setup(ms => ms.GetTotpMaxTimeSteps())
            .Returns(3);
        _authTokensService = new Mock<IAuthTokensService>();
        var websiteUiService = new Mock<IWebsiteUiService>();
        _repository = new Mock<IPersonCredentialRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()))
            .Returns((PersonCredentialRoot root, CancellationToken _) =>
                Task.FromResult<Result<PersonCredentialRoot, Error>>(root));
        _repository.Setup(rep =>
                rep.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((PersonCredentialRoot root, bool _, CancellationToken _) =>
                Task.FromResult<Result<PersonCredentialRoot, Error>>(root));

        _service = new NativeIdentityServerCredentialsService(_recorder.Object, _idFactory.Object,
            _endUsersService.Object,
            _userProfilesService.Object, _userNotificationsService.Object, _settings.Object,
            _emailAddressService.Object,
            _tokensService.Object, _encryptionService.Object, _passwordHasherService.Object, _mfaService.Object,
            _authTokensService.Object, websiteUiService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenChangeMfaForUserAsyncAndUserNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ChangeMfaForUserAsync(_caller.Object, "auserid", true, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeMfaForUserAsyncAndNotAPerson_ThenReturnsError()
    {
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
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
            await _service.ChangeMfaForUserAsync(_caller.Object, "auserid", true, CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialsApplication_NotPerson);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeMfaForUserAsync_ThenEnablesMfa()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
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
            await _service.ChangeMfaForUserAsync(_caller.Object, "auserid", true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsMfaEnabled.Should().BeTrue();
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(root =>
            root.MfaOptions.IsEnabled
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsForUserAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result =
            await _service.ListMfaAuthenticatorsForUserAsync(_caller.Object, "auserid", null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsForUserAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ListMfaAuthenticatorsForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsForUserAsyncByAuthenticatedUserButNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ListMfaAuthenticatorsForUserAsync(_caller.Object, "auserid", null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsForUserAsyncByAnonymous_ThenReturnsAuthenticators()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.ListMfaAuthenticatorsForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsForUserAsyncByAuthenticatedUser_ThenReturnsAuthenticators()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.ListMfaAuthenticatorsForUserAsync(_caller.Object, "auserid", null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorForUserAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.DisassociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anauthenticatorid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorForUserAsync_ThenDeletes()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        var authenticator = await credential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.DisassociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", authenticator.Value.Id,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.HasNone()
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAuthenticatedUserButNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAnonymousForTotpAndNotAPerson_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Machine
            });

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialsApplication_NotPerson);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAnonymousForTotp_ThenAssociates()
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
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().BeNull();
        result.Value.BarCodeUri.Should().Be("abarcodeuri");
        result.Value.Secret.Should().Be("anotpsecret");
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive == false
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.TotpAuthenticator
            && cred.MfaAuthenticators[1].OobCode == Optional<string>.None
            && cred.MfaAuthenticators[1].BarCodeUri == "abarcodeuri"
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAuthenticatedUserForTotp_ThenAssociates()
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
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().BeNull();
        result.Value.BarCodeUri.Should().Be("abarcodeuri");
        result.Value.Secret.Should().Be("anotpsecret");
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive == false
            && cred.MfaAuthenticators[1].Type == MfaAuthenticatorType.TotpAuthenticator
            && cred.MfaAuthenticators[1].OobCode == Optional<string>.None
            && cred.MfaAuthenticators[1].BarCodeUri == "abarcodeuri"
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAuthenticatedUserForOobSms_ThenAssociates()
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
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.OobSms, "+6498876986",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().Be("anoobcode");
        result.Value.BarCodeUri.Should().BeNull();
        result.Value.Secret.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive == false
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
                "anoobsecret", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForUserAsyncByAuthenticatedUserForOobEmail_ThenAssociates()
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
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.OobEmail, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        result.Value.RecoveryCodes.Should().NotBeEmpty();
        result.Value.OobCode.Should().Be("anoobcode");
        result.Value.BarCodeUri.Should().BeNull();
        result.Value.Secret.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[0].Type == MfaAuthenticatorType.RecoveryCodes
            && cred.MfaAuthenticators[1].IsActive == false
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
                "anoobsecret", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task
        WhenAssociateMfaAuthenticatorForUserAsyncForSecondAuthenticator_ThenAssociatesWithoutRecoveryCodes()
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
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
            CredentialMfaAuthenticatorType.OobSms, "auser@company.com",
            CancellationToken.None);

        var result =
            await _service.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.OobEmail, null,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        result.Value.RecoveryCodes.Should().BeNull();
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
        _userNotificationsService.Verify(ns =>
            ns.NotifyPasswordMfaOobEmailAsync(_caller.Object, "auser@company.com",
                "anoobsecret", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task
        WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAuthenticatedUserButNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAnonymousForTotp_ThenConfirms()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, EmailAddress.Create("auser@company.com").Value,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, null, "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Tokens!.UserId.Should().Be("auserid");
        result.Value.Tokens!.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.Tokens!.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.Tokens!.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.Tokens!.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[1].VerifiedState == "1"
            && cred.MfaAuthenticators[1].IsActive == true
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAuthenticatedUserForTotp_ThenConfirms()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, EmailAddress.Create("auser@company.com").Value,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Memberships =
            [
                new Membership
                {
                    Id = "amembershipid",
                    IsDefault = true,
                    OrganizationId = "anorganizationid",
                    UserId = "auserid"
                }
            ]
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.TotpAuthenticator, null, "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Tokens.Should().BeNull();
        result.Value.Authenticators!.Count.Should().Be(2);
        result.Value.Authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
        result.Value.Authenticators[0].IsActive.Should().BeTrue();
        result.Value.Authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.Authenticators[1].IsActive.Should().BeTrue();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[1].VerifiedState == "1"
            && cred.MfaAuthenticators[1].IsActive == true
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAuthenticatedUserForOobSms_ThenConfirms()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Memberships =
            [
                new Membership
                {
                    Id = "amembershipid",
                    IsDefault = true,
                    OrganizationId = "anorganizationid",
                    UserId = "auserid"
                }
            ]
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Tokens.Should().BeNull();
        result.Value.Authenticators!.Count.Should().Be(2);
        result.Value.Authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
        result.Value.Authenticators[0].IsActive.Should().BeTrue();
        result.Value.Authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        result.Value.Authenticators[1].IsActive.Should().BeTrue();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].IsActive == true
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForUserAsyncByAuthenticatedUserForOobEmail_ThenConfirms()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail,
            Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Memberships =
            [
                new Membership
                {
                    Id = "amembershipid",
                    IsDefault = true,
                    OrganizationId = "anorganizationid",
                    UserId = "auserid"
                }
            ]
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object, "auserid", null,
                CredentialMfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Tokens.Should().BeNull();
        result.Value.Authenticators!.Count.Should().Be(2);
        result.Value.Authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
        result.Value.Authenticators[0].IsActive.Should().BeTrue();
        result.Value.Authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        result.Value.Authenticators[1].IsActive.Should().BeTrue();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[0].IsActive == true
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].IsActive == true
        ), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorForUserAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ChallengeMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                "anauthenticatorid", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorForUserAsyncByAnonymousForTotpAuthenticator_ThenChallenges()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, EmailAddress.Create("auser@company.com").Value,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator,
            null, "aconfirmationcode");
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.ChallengeMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                "anauthenticatorid2", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        result.Value.OobCode.Should().BeNull();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == "1"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorForUserAsyncByAnonymousForOobSms_ThenChallenges()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.OobSms,
            "anoobcode", "anoobsecret");
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.ChallengeMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                "anauthenticatorid2", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        result.Value.OobCode.Should().Be("anoobcode");
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorForUserAsyncByAnonymousForOobEmail_ThenChallenges()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail,
            Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.OobEmail,
            "anoobcode", "anoobsecret");
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.ChallengeMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                "anauthenticatorid2", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        result.Value.OobCode.Should().Be("anoobcode");
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAnonymousAndNoMfaToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", null!,
            CredentialMfaAuthenticatorType.OobEmail, "anoobcode", "aconfirmationcode", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAnonymousAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.OobEmail, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAuthenticatedUser_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, EmailAddress.Create("auser@company.com").Value,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.OobEmail, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAuthenticatedUserForRecoveryCodes_ThenConfirms()
    {
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        //We need to create another authenticator to get recovery codes created
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<PhoneNumber>.None, Optional<EmailAddress>.None, EmailAddress.Create("auser@company.com").Value,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, null,
            "aconfirmationcode");
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });
        var recoveryCode = credential.MfaAuthenticators.ToRecoveryCodes(_encryptionService.Object)[0];

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.RecoveryCodes, null, recoveryCode, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == recoveryCode
            && cred.MfaAuthenticators[1].VerifiedState == "1"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAuthenticatedUserForTotp_ThenConfirms()
    {
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        var authenticator = await credential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, null,
            "aconfirmationcode");
        await credential.ChallengeMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), null).Value,
            authenticator.Value.Id,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, null, "aconfirmationcode",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == "1"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAuthenticatedUserForOobSms_ThenConfirms()
    {
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        var authenticator = await credential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.OobSms, "anoobcode",
            "anoobsecret");
        await credential.ChallengeMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), null).Value,
            authenticator.Value.Id,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForUserAsyncByAuthenticatedUserForOobEmail_ThenConfirms()
    {
        var credential = CreateVerifiedCredential();
        credential.ChangeMfaEnabled("auserid".ToId(), true);
        credential.InitiateMfaAuthentication();
        var authenticator = await credential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.OobEmail, "anoobcode",
            "anoobsecret");
        await credential.ChallengeMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), null).Value,
            authenticator.Value.Id,
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(s =>
                s.FindCredentialByMfaAuthenticationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Dictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result =
            await _service.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "auserid", "anmfatoken",
                CredentialMfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.MfaAuthenticators.Count == 2
            && cred.MfaAuthenticators[0].VerifiedState == Optional<string>.None
            && cred.MfaAuthenticators[1].VerifiedState == Optional<string>.None
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenResetPasswordMfaForUserAsyncByOperatorAndUserNotExist_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ResetPasswordMfaForUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenResetPasswordMfaForUserAsyncAndNotAPerson_ThenReturnsError()
    {
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
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
            await _service.ResetPasswordMfaForUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialsApplication_NotPerson);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenResetPasswordMfaForUserAsync_ThenResetsMfaState()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("anoperatorid");
        _caller.Setup(cc => cc.Roles).Returns(new ICallerContext.CallerRoles([PlatformRoles.Operations], []));
        var credential = CreateVerifiedCredential();
        _repository.Setup(s =>
                s.FindCredentialByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
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
            await _service.ResetPasswordMfaForUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsMfaEnabled.Should().BeFalse();
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid",
                It.IsAny<CancellationToken>()));
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(root =>
            root.MfaOptions.IsEnabled == false
            && root.MfaOptions.CanBeDisabled == true
        ), It.IsAny<CancellationToken>()));
    }

    private PersonCredentialRoot CreateUnVerifiedCredential()
    {
        var credential = CreateCredential();
        credential.SetCredentials("apassword");
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();

        return credential;
    }

    private PersonCredentialRoot CreateVerifiedCredential()
    {
        var credential = CreateUnVerifiedCredential();
        credential.VerifyRegistration();
        return credential;
    }

    private PersonCredentialRoot CreateCredential()
    {
        return PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object, _settings.Object,
            _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId()).Value;
    }
}