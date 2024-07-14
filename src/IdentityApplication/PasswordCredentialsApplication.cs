using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Resources.Shared.Extensions;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication;

public class PasswordCredentialsApplication : IPasswordCredentialsApplication
{
    private const string ProviderName = "credentials";
#if TESTINGONLY
    private const double MinAuthenticateDelayInSecs = 0;
    private const double MaxAuthenticateDelayInSecs = 0;
#else
    private const double MinAuthenticateDelayInSecs = 1.5;
    private const double MaxAuthenticateDelayInSecs = 4.0;
#endif
    private readonly IEndUsersService _endUsersService;
    private readonly IUserNotificationsService _userNotificationsService;
    private readonly IConfigurationSettings _settings;
    private readonly IEmailAddressService _emailAddressService;
    private readonly ITokensService _tokensService;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IAuthTokensService _authTokensService;
    private readonly IWebsiteUiService _websiteUiService;
    private readonly IRecorder _recorder;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IPasswordCredentialsRepository _repository;
    private readonly IDelayGenerator _delayGenerator;
    private readonly IUserProfilesService _userProfilesService;

    public PasswordCredentialsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService,
        IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPasswordCredentialsRepository repository) : this(recorder, identifierFactory, endUsersService,
        userProfilesService, userNotificationsService, settings, emailAddressService, tokensService,
        passwordHasherService, authTokensService, websiteUiService, repository, new DelayGenerator())
    {
        _recorder = recorder;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    private PasswordCredentialsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService,
        IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPasswordCredentialsRepository repository,
        IDelayGenerator delayGenerator)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _endUsersService = endUsersService;
        _userProfilesService = userProfilesService;
        _userNotificationsService = userNotificationsService;
        _settings = settings;
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        _authTokensService = authTokensService;
        _websiteUiService = websiteUiService;
        _repository = repository;
        _delayGenerator = delayGenerator;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string username,
        string password, CancellationToken cancellationToken)
    {
        await DelayForRandomPeriodAsync(MinAuthenticateDelayInSecs, MaxAuthenticateDelayInSecs, cancellationToken);

        var retrievedCredentials = await _repository.FindCredentialsByUsernameAsync(username, cancellationToken);
        if (retrievedCredentials.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        if (!retrievedCredentials.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var credentials = retrievedCredentials.Value.Value;
        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, credentials.UserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
        if (user.Status != EndUserStatus.Registered)
        {
            return Error.NotAuthenticated();
        }

        if (user.Classification != EndUserClassification.Person)
        {
            return Error.NotAuthenticated();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate a password with a suspended account", user.Id);
            return Error.EntityExists(Resources.PasswordCredentialsApplication_AccountSuspended);
        }

        if (credentials.IsLocked)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_AccountLocked,
                "User {Id} tried to authenticate a password with a locked account", user.Id);
            return Error.EntityExists(Resources.PasswordCredentialsApplication_AccountLocked);
        }

        var verifyPassword = await VerifyPasswordAsync();
        if (verifyPassword.IsFailure)
        {
            return verifyPassword.Error;
        }

        var isVerified = verifyPassword.Value;
        if (!isVerified)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_InvalidCredentials,
                "User {Id} failed to authenticate with an invalid password", user.Id);
            return Error.NotAuthenticated();
        }

        if (!credentials.IsVerified)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_BeforeVerified,
                "User {Id} tried to authenticate with a password before verifying their registration", user.Id);
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_RegistrationNotVerified);
        }

        var maintenance = Caller.CreateAsMaintenance(caller.CallId);
        var profiled = await _userProfilesService.GetProfilePrivateAsync(maintenance, user.Id, cancellationToken);
        if (profiled.IsFailure)
        {
            return profiled.Error;
        }

        var profile = profiled.Value;
        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.PasswordCredentialsApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with a password", user.Id);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id, UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(ProviderName, profile));

        var issued = await _authTokensService.IssueTokensAsync(caller, user, cancellationToken);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        return new Result<AuthenticateTokens, Error>(new AuthenticateTokens
        {
            AccessToken = new AuthenticateToken
            {
                Value = tokens.AccessToken,
                ExpiresOn = tokens.AccessTokenExpiresOn
            },
            RefreshToken = new AuthenticateToken
            {
                Value = tokens.RefreshToken,
                ExpiresOn = tokens.RefreshTokenExpiresOn
            },
            UserId = user.Id
        });

        async Task<Result<bool, Error>> VerifyPasswordAsync()
        {
            var verify = credentials.VerifyPassword(password);
            if (verify.IsFailure)
            {
                return verify.Error;
            }

            var saved1 = await _repository.SaveAsync(credentials, cancellationToken);
            if (saved1.IsFailure)
            {
                return saved1.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Credentials were verified for {Id}", saved1.Value.Id);

            return verify.Value;
        }
    }

    public async Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByUsernameAsync(emailAddress, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            var warned =
                await _userNotificationsService.NotifyPasswordResetUnknownUserCourtesyAsync(caller, emailAddress,
                    cancellationToken);
            if (warned.IsFailure)
            {
                return warned.Error;
            }

            return Result.Ok;
        }

        var credentials = retrieved.Value.Value;
        var initiated = credentials.InitiatePasswordReset();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var registration = credentials.Registration.Value;
        var notified = await _userNotificationsService.NotifyPasswordResetInitiatedAsync(caller, registration.Name,
            emailAddress, credentials.Password.ResetToken, cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password reset initiated for {Id}", credentials.UserId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserPasswordForgotten,
            new Dictionary<string, object>
            {
                { nameof(PasswordCredential.Id), credentials.UserId }
            });

        return Result.Ok;
    }

    public async Task<Result<PasswordCredential, Error>> RegisterPersonAsync(ICallerContext caller,
        string? invitationToken, string firstName,
        string lastName, string emailAddress, string password, string? timezone, string? countryCode,
        bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        var registered = await _endUsersService.RegisterPersonPrivateAsync(caller, invitationToken, emailAddress,
            firstName, lastName, timezone, countryCode, termsAndConditionsAccepted, cancellationToken);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        return await RegisterPersonInternalAsync(caller, emailAddress, password, firstName, registered.Value,
            cancellationToken);
    }

    public async Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var initiated = credentials.InitiatePasswordReset();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var registration = credentials.Registration.Value;
        var notified = await _userNotificationsService.NotifyPasswordResetInitiatedAsync(caller, registration.Name,
            registration.EmailAddress, credentials.Password.ResetToken, cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password reset re-initiated for {Id}", credentials.UserId);

        return Result.Ok;
    }

    public async Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var verified = credentials.VerifyPasswordReset(token);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Password reset verified for {Id}", credentials.UserId);

        return Result.Ok;
    }

    public async Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var reset = credentials.CompletePasswordReset(token, password);
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password was reset for {Id}", credentials.UserId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserPasswordReset,
            new Dictionary<string, object>
            {
                { nameof(credentials.Id), credentials.UserId }
            });

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<PasswordCredentialConfirmation, Error>> GetPersonRegistrationConfirmationAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        var token = credential.VerificationKeep.Token;

        if (!token.HasValue)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_RegistrationAlreadyVerified);
        }

        return new PasswordCredentialConfirmation
        {
            Token = credential.VerificationKeep.Token,
            Url = _websiteUiService.ConstructPasswordRegistrationConfirmationPageUrl(credential.VerificationKeep.Token)
        };
    }
#endif

    public async Task<Result<Error>> ConfirmPersonRegistrationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByRegistrationVerificationTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        var verified = credential.VerifyRegistration();
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Password credentials for {UserId} have been verified",
            credential.UserId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationConfirmed,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId }
            });

        return Result.Ok;
    }

    private async Task<Result<PasswordCredential, Error>> RegisterPersonInternalAsync(ICallerContext caller,
        string emailAddress, string password, string displayName, EndUser user, CancellationToken cancellationToken)
    {
        var fetched = await _repository.FindCredentialsByUserIdAsync(user.Id.ToId(), cancellationToken);
        if (fetched.IsFailure)
        {
            return fetched.Error;
        }

        if (fetched.Value.HasValue)
        {
            return fetched.Value.Value.ToCredential(user);
        }

        var created = PasswordCredentialRoot.Create(_recorder, _identifierFactory, _settings, _emailAddressService,
            _tokensService, _passwordHasherService, user.Id.ToId());
        if (created.IsFailure)
        {
            return created.Error;
        }

        var email = EmailAddress.Create(emailAddress);
        if (email.IsFailure)
        {
            return email.Error;
        }

        var name = PersonDisplayName.Create(displayName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var credentials = created.Value;
        var credentialed = credentials.SetPasswordCredential(password);
        if (credentialed.IsFailure)
        {
            return credentialed.Error;
        }

        var registered = credentials.SetRegistrationDetails(email.Value, name.Value);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        var initiated = credentials.InitiateRegistrationVerification();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var notified = await _userNotificationsService.NotifyPasswordRegistrationConfirmationAsync(caller,
            credentials.Registration.Value.EmailAddress,
            credentials.Registration.Value.Name, credentials.VerificationKeep.Token, cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password credentials created for {UserId}", credentials.UserId);

        return credentials.ToCredential(user);
    }

    /// <summary>
    ///     Provides a random time delay to mitigate against authentication timing attacks
    /// </summary>
    private async Task DelayForRandomPeriodAsync(double fromSeconds, double toSeconds,
        CancellationToken cancellationToken)
    {
        if (Math.Abs(fromSeconds - toSeconds) < 1)
        {
            return;
        }

        var delay = _delayGenerator.GetNextRandom(fromSeconds, toSeconds);
        await Task.Delay(delay, cancellationToken);
    }
}

internal static class PasswordCredentialConversionExtensions
{
    public static PasswordCredential ToCredential(this PasswordCredentialRoot credential, EndUser user)
    {
        return new PasswordCredential
        {
            Id = credential.Id,
            User = user
        };
    }

    public static Dictionary<string, object> ToLoginUserUsage(this EndUserWithMemberships user, string providerName,
        UserProfile profile)
    {
        var context = new Dictionary<string, object>
        {
            { UsageConstants.Properties.AuthProvider, providerName },
            { UsageConstants.Properties.UserIdOverride, user.Id },
            { UsageConstants.Properties.Name, profile.Name.FullName() }
        };
        if (profile.EmailAddress.HasValue())
        {
            context.Add(UsageConstants.Properties.EmailAddress, profile.EmailAddress);
        }

        var defaultMembership = user.Memberships.FirstOrDefault(ms => ms.IsDefault);
        if (defaultMembership.Exists())
        {
            context.Add(UsageConstants.Properties.DefaultOrganizationId, defaultMembership.Id);
        }

        return context;
    }
}