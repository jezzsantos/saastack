using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
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
public class PasswordCredentialsApplicationSpec
{
    private readonly PasswordCredentialsApplication _application;
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<INotificationsService> _notificationsService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPasswordCredentialsRepository> _repository;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;

    public PasswordCredentialsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _endUsersService = new Mock<IEndUsersService>();
        _notificationsService = new Mock<INotificationsService>();
        _settings = new Mock<IConfigurationSettings>();
        _settings.Setup(s => s.Platform.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null!);
        _settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns(5);
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .Returns(Task.FromResult(true));
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateTokenForVerification())
            .Returns("averificationtoken");
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns("apasswordhash");
        _passwordHasherService.Setup(phs => phs.ValidatePasswordHash(It.IsAny<string>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _authTokensService = new Mock<IAuthTokensService>();
        var websiteUiService = new Mock<IWebsiteUiService>();
        _repository = new Mock<IPasswordCredentialsRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()))
            .Returns((PasswordCredentialRoot root, CancellationToken _) =>
                Task.FromResult<Result<PasswordCredentialRoot, Error>>(root));

        _application = new PasswordCredentialsApplication(_recorder.Object, _idFactory.Object, _endUsersService.Object,
            _notificationsService.Object, _settings.Object, _emailAddressService.Object, _tokensService.Object,
            _passwordHasherService.Object, _authTokensService.Object, websiteUiService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndNoCredentials_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(Optional<PasswordCredentialRoot>
                .None));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndUnknownUser_ThenReturnsError()
    {
        var credential = CreateCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(Error.EntityNotFound()));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndEndUserIsNotRegistered_ThenReturnsError()
    {
        var credential = CreateCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(new EndUserWithMemberships
            {
                Id = "anid",
                Status = EndUserStatus.Unregistered
            }));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndEndUserIsSuspended_ThenReturnsError()
    {
        var credential = CreateCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(new EndUserWithMemberships
            {
                Id = "auserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Suspended
            }));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.PasswordCredentialsApplication_AccountSuspended);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.PasswordCredentialsApplication_Authenticate_AccountSuspended, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndCredentialsIsLocked_ThenReturnsError()
    {
        var credential = CreateVerifiedCredential();
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
#if TESTINGONLY
        credential.TestingOnly_LockAccount("awrongpassword");
#endif
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(new EndUserWithMemberships
            {
                Id = "auserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled
            }));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.PasswordCredentialsApplication_AccountLocked);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.PasswordCredentialsApplication_Authenticate_AccountLocked, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndWrongPassword_ThenReturnsError()
    {
        var credential = CreateUnVerifiedCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(new EndUserWithMemberships
            {
                Id = "auserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled
            }));
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "awrongpassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.PasswordCredentialsApplication_Authenticate_InvalidCredentials, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndCredentialsNotYetVerified_ThenReturnsError()
    {
        var credential = CreateUnVerifiedCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(new EndUserWithMemberships
            {
                Id = "auserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled
            }));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsApplication_RegistrationNotVerified);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.PasswordCredentialsApplication_Authenticate_BeforeVerified, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncWithCorrectPassword_ThenReturnsError()
    {
        var credential = CreateVerifiedCredential();
        _repository.Setup(rep => rep.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(user));
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<EndUserWithMemberships>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessTokens, Error>>(new AccessTokens("anaccesstoken", expiresOn,
                "arefreshtoken", expiresOn)));

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.PasswordCredentialsApplication_Authenticate_Succeeded, It.IsAny<string>(),
            It.IsAny<object[]>()));
        _authTokensService.Verify(jts =>
            jts.IssueTokensAsync(It.IsAny<ICallerContext>(), user, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRegisterPersonUserAccountAndAlreadyExists_ThenDoesNothing()
    {
        var endUser = new RegisteredEndUser
        {
            Id = "auserid"
        };
        _endUsersService.Setup(uas => uas.RegisterPersonAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<RegisteredEndUser, Error>>(endUser));
        var credential = CreateUnVerifiedCredential();
        _repository.Setup(s => s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));

        var result = await _application.RegisterPersonAsync(_caller.Object, "afirstname",
            "alastname", "auser@company.com", "apassword", "atimezone", "acountrycode", true, CancellationToken.None);

        result.Value.User.Should().Be(endUser);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _endUsersService.Verify(uas => uas.RegisterPersonAsync(_caller.Object,
            "auser@company.com", "afirstname", "alastname", "atimezone", "acountrycode", true,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRegisterPersonUserAccountAndNotExists_ThenCreatesAndSendsConfirmation()
    {
        var registeredAccount = new RegisteredEndUser
        {
            Id = "auserid",
            Profile = new ProfileWithDefaultMembership
            {
                Id = "anid",
                Name = new PersonName
                {
                    FirstName = "aname"
                },
                DisplayName = "adisplayname",
                EmailAddress = "auser@company.com"
            }
        };
        _endUsersService.Setup(uas => uas.RegisterPersonAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<RegisteredEndUser, Error>>(registeredAccount));
        _repository.Setup(s => s.FindCredentialsByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(Optional<PasswordCredentialRoot>
                .None));

        var result = await _application.RegisterPersonAsync(_caller.Object, "afirstname",
            "alastname", "auser@company.com", "apassword", "atimezone", "acountrycode", true, CancellationToken.None);

        result.Value.User.Should().Be(registeredAccount);
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(uc =>
            uc.Id == "anid"
            && uc.UserId == "auserid"
            && uc.Registration.Value.Name == "adisplayname"
            && uc.Registration.Value.EmailAddress == "auser@company.com"
            && uc.Password.PasswordHash == "apasswordhash"
            && uc.Login.Exists()
            && !uc.Verification.IsVerified
        ), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns =>
            ns.NotifyPasswordRegistrationConfirmationAsync(_caller.Object, "auser@company.com", "adisplayname",
                "averificationtoken", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus => eus.RegisterPersonAsync(_caller.Object,
            "auser@company.com", "afirstname", "alastname", "atimezone", "acountrycode", true,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenConfirmPersonRegistrationAsyncAndTokenUnknown_ThenReturnsError()
    {
        _repository.Setup(s => s.FindCredentialsByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(Optional<PasswordCredentialRoot>
                .None));

        var result =
            await _application.ConfirmPersonRegistrationAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(s => s.SaveAsync(It.IsAny<PasswordCredentialRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenConfirmPersonRegistrationAsync_ThenReturnsSuccess()
    {
        var credential = CreateUnVerifiedCredential();
        _repository.Setup(s => s.FindCredentialsByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<PasswordCredentialRoot>, Error>>(credential.ToOptional()));

        var result =
            await _application.ConfirmPersonRegistrationAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(s => s.SaveAsync(It.Is<PasswordCredentialRoot>(pc =>
            pc.IsVerified
            && pc.IsRegistrationVerified
        ), It.IsAny<CancellationToken>()));
    }

    private PasswordCredentialRoot CreateUnVerifiedCredential()
    {
        var credential = CreateCredential();
        credential.SetCredential("apassword");
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
            "auserid".ToId()).Value;
    }
}