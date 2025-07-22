using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerCredentialsServicePasswordResetSpec
{
    private const string TestingToken = "Ll4qhv77XhiXSqsTUc6icu56ZLrqu5p1gH9kT5IlHio";
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IMfaService> _mfaService;
    private readonly Mock<IUserNotificationsService> _notificationsService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPersonCredentialRepository> _repository;
    private readonly NativeIdentityServerCredentialsService _service;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;

    public NativeIdentityServerCredentialsServicePasswordResetSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        var endUsersService = new Mock<IEndUsersService>();
        var userProfilesService = new Mock<IUserProfilesService>();
        _notificationsService = new Mock<IUserNotificationsService>();
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
        var authTokensService = new Mock<IAuthTokensService>();
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
            endUsersService.Object,
            userProfilesService.Object, _notificationsService.Object, _settings.Object, _emailAddressService.Object,
            _tokensService.Object, _encryptionService.Object, _passwordHasherService.Object, _mfaService.Object,
            authTokensService.Object,
            websiteUiService.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenInitiatePasswordRequestAndUnknownEmailAddress_ThenSendsCourtesyNotification()
    {
        _repository.Setup(s => s.FindCredentialByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.InitiatePasswordResetAsync(_caller.Object, "user@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetUnknownUserCourtesyAsync(_caller.Object, "user@company.com",
                UserNotificationConstants.EmailTags.PasswordResetUnknownUser, CancellationToken.None));
    }

    [Fact]
    public async Task WhenInitiatePasswordRequest_ThenSendsNotification()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(TestingToken);
        _repository.Setup(s => s.FindCredentialByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVerifiedCredential().ToOptional());

        var result =
            await _service.InitiatePasswordResetAsync(_caller.Object, "user@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.IsPasswordSet
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(_caller.Object, "aname", "user@company.com", TestingToken,
                UserNotificationConstants.EmailTags.PasswordResetInitiated, It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetUnknownUserCourtesyAsync(It.IsAny<ICallerContext>(), "user@company.com",
                It.IsAny<IReadOnlyList<string>>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task WhenResendPasswordRequestAndUnknownToken_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.ResendPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenResendPasswordRequest_ThenResendsNotification()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(TestingToken);
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVerifiedCredential().ToOptional());

        var result =
            await _service.ResendPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(cred =>
            cred.IsPasswordResetInitiated
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(_caller.Object, "aname", "auser@company.com", TestingToken,
                UserNotificationConstants.EmailTags.PasswordResetResend, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenVerifyPasswordResetAsyncAndUnknownToken_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.VerifyPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenVerifyPasswordResetAsync_ThenVerifies()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(TestingToken);
        var credential = CreateVerifiedCredential();
        credential.InitiatePasswordReset();
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.VerifyPasswordResetAsync(_caller.Object, TestingToken, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenCompletePasswordResetAsyncAndUnknownToken_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>.None);

        var result =
            await _service.CompletePasswordResetAsync(_caller.Object, "atoken", "apassword",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PersonCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenCompletePasswordResetAsync_ThenCompletes()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(TestingToken);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var credential = CreateVerifiedCredential();
        credential.InitiatePasswordReset();
        _repository.Setup(s =>
                s.FindCredentialByPasswordResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result =
            await _service.CompletePasswordResetAsync(_caller.Object, TestingToken, "2Password!",
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PersonCredentialRoot>(creds =>
            !creds.IsPasswordResetInitiated
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordResetInitiatedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
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