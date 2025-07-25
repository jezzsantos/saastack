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
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native credentials service for managing and persisting credentials and MFA
/// </summary>
public partial class NativeIdentityServerCredentialsService : IIdentityServerCredentialsService
{
    public const string ProviderName = "credentials";
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
    private readonly IMfaService _mfaService;
    private readonly IAuthTokensService _authTokensService;
    private readonly IWebsiteUiService _websiteUiService;
    private readonly IRecorder _recorder;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IPersonCredentialRepository _repository;
    private readonly IDelayGenerator _delayGenerator;
    private readonly IUserProfilesService _userProfilesService;
    private readonly IEncryptionService _encryptionService;

    public NativeIdentityServerCredentialsService(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService,
        IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService, IEncryptionService encryptionService,
        IPasswordHasherService passwordHasherService, IMfaService mfaService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPersonCredentialRepository repository) : this(recorder, identifierFactory, endUsersService,
        userProfilesService, userNotificationsService, settings, emailAddressService, tokensService, encryptionService,
        passwordHasherService, mfaService, authTokensService, websiteUiService, repository, new DelayGenerator())
    {
        _recorder = recorder;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    private NativeIdentityServerCredentialsService(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService,
        IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService, IEncryptionService encryptionService,
        IPasswordHasherService passwordHasherService, IMfaService mfaService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPersonCredentialRepository repository,
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
        _encryptionService = encryptionService;
        _passwordHasherService = passwordHasherService;
        _mfaService = mfaService;
        _authTokensService = authTokensService;
        _websiteUiService = websiteUiService;
        _repository = repository;
        _delayGenerator = delayGenerator;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string username,
        string password, CancellationToken cancellationToken)
    {
        await DelayForRandomPeriodAsync(MinAuthenticateDelayInSecs, MaxAuthenticateDelayInSecs, cancellationToken);

        var retrievedCredentials = await _repository.FindCredentialByUsernameAsync(username, cancellationToken);
        if (retrievedCredentials.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        if (!retrievedCredentials.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var credential = retrievedCredentials.Value.Value;
        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, credential.UserId, cancellationToken);
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
                Audits.PersonCredentialsApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate a password with a suspended account", user.Id);
            return Error.EntityLocked(Resources.PersonCredentialsApplication_AccountSuspended);
        }

        if (credential.IsLocked)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PersonCredentialsApplication_Authenticate_AccountLocked,
                "User {Id} tried to authenticate a password with a locked account", user.Id);
            return Error.EntityLocked(Resources.PersonCredentialsApplication_AccountLocked);
        }

        var verified = credential.VerifyPassword(password);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var saved = await _repository.SaveAsync(credential, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Credentials were verified for {Id}", saved.Value.Id);
        credential = saved.Value;
        var isVerified = verified.Value;
        if (!isVerified)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PersonCredentialsApplication_Authenticate_InvalidCredentials,
                "User {Id} failed to authenticate with an invalid password", user.Id);
            return Error.NotAuthenticated();
        }

        if (!credential.IsVerified)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.PersonCredentialsApplication_Authenticate_BeforeVerified,
                "User {Id} tried to authenticate with a password before verifying their registration", user.Id);
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_RegistrationNotVerified);
        }

        var maintenance = Caller.CreateAsMaintenance(caller);
        var profiled = await _userProfilesService.GetProfilePrivateAsync(maintenance, user.Id, cancellationToken);
        if (profiled.IsFailure)
        {
            return profiled.Error;
        }

        var profile = profiled.Value;
        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.PersonCredentialsApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with a password", user.Id);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id, UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(ProviderName, profile));

        if (credential.IsMfaEnabled)
        {
            var initiated = credential.InitiateMfaAuthentication();
            if (initiated.IsFailure)
            {
                return initiated.Error;
            }

            var mfaToken = initiated.Value;
            saved = await _repository.SaveAsync(credential, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            return Error.ForbiddenAccess(Resources.PersonCredentialsApplication_MfaRequired, MfaRequiredCode,
                new Dictionary<string, object>
                {
                    { MfaTokenName, mfaToken }
                });
        }

        return await IssueAuthenticationTokensAsync(caller, user, cancellationToken);
    }

    public async Task<Result<PersonCredential, Error>> RegisterPersonAsync(ICallerContext caller,
        string? invitationToken, string firstName, string lastName, string emailAddress, string password,
        string? timezone, string? countryCode, bool termsAndConditionsAccepted, CancellationToken cancellationToken)
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

    public async Task<Result<PersonCredential, Error>> GetPersonCredentialForUserAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_NotPerson);
        }

        _recorder.TraceInformation(caller.ToCall(), "Password credentials for {UserId} has been retrieved",
            credential.UserId);

        return credential.ToCredential(user);
    }

#if TESTINGONLY
    public async Task<Result<PersonCredentialEmailConfirmation, Error>> GetPersonRegistrationConfirmationForUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialByUserIdAsync(userId.ToId(), cancellationToken);
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
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_RegistrationAlreadyVerified);
        }

        return new PersonCredentialEmailConfirmation
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

    private async Task<Result<AuthenticateTokens, Error>> IssueAuthenticationTokensAsync(ICallerContext caller,
        EndUserWithMemberships user, CancellationToken cancellationToken)
    {
        var issued = await _authTokensService.IssueTokensAsync(caller, user, cancellationToken);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        return new Result<AuthenticateTokens, Error>(new AuthenticateTokens
        {
            AccessToken = new AuthenticationToken
            {
                Value = tokens.AccessToken,
                ExpiresOn = tokens.AccessTokenExpiresOn,
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = tokens.RefreshToken,
                ExpiresOn = tokens.RefreshTokenExpiresOn,
                Type = TokenType.RefreshToken
            },
            UserId = user.Id
        });
    }

    private async Task<Result<PersonCredential, Error>> RegisterPersonInternalAsync(ICallerContext caller,
        string emailAddress, string password, string displayName, EndUserWithProfile user,
        CancellationToken cancellationToken)
    {
        var userId = user.Id;
        var retrievedCredential = await _repository.FindCredentialByUserIdAsync(user.Id.ToId(), cancellationToken);
        if (retrievedCredential.IsFailure)
        {
            return retrievedCredential.Error;
        }

        if (retrievedCredential.Value.HasValue)
        {
            var profile = user.Profile;
            if (user is { Status: EndUserStatus.Registered, Classification: EndUserClassification.Person }
                && profile.Exists())
            {
                var notified = await _userNotificationsService.NotifyPasswordRegistrationRepeatCourtesyAsync(caller,
                    userId, profile.EmailAddress!, profile.DisplayName, profile.Timezone,
                    profile.Address.CountryCode,
                    UserNotificationConstants.EmailTags.RegistrationRepeatCourtesy, cancellationToken);
                if (notified.IsFailure)
                {
                    return notified.Error;
                }

                _recorder.TraceInformation(caller.ToCall(),
                    "Attempted re-registration of user: {Id}, with email {EmailAddress}", userId, emailAddress);
                _recorder.TrackUsage(caller.ToCall(),
                    UsageConstants.Events.UsageScenarios.Generic.PersonReRegistered,
                    new Dictionary<string, object>
                    {
                        { UsageConstants.Properties.Id, userId },
                        { UsageConstants.Properties.EmailAddress, emailAddress }
                    });
            }

            return retrievedCredential.Value.Value.ToCredential(user);
        }

        var created = PersonCredentialRoot.Create(_recorder, _identifierFactory, _settings, _emailAddressService,
            _tokensService, _encryptionService, _passwordHasherService, _mfaService, userId.ToId());
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

        var credential = created.Value;
        var credentialed = credential.SetCredentials(password);
        if (credentialed.IsFailure)
        {
            return credentialed.Error;
        }

        var registered = credential.SetRegistrationDetails(email.Value, name.Value);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        var initiated = credential.InitiateRegistrationVerification();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var confirmed = await _userNotificationsService.NotifyPasswordRegistrationConfirmationAsync(caller,
            credential.Registration.Value.EmailAddress, credential.Registration.Value.Name,
            credential.VerificationKeep.Token, UserNotificationConstants.EmailTags.RegisterPerson, cancellationToken);
        if (confirmed.IsFailure)
        {
            return confirmed.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password credentials created for {UserId}", credential.UserId);

        return credential.ToCredential(user);
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

internal static class NativeIdentityServerCredentialsServiceConversionExtensions
{
    public static PersonCredential ToCredential(this PersonCredentialRoot personCredential, EndUser user)
    {
        return new PersonCredential
        {
            Id = personCredential.Id,
            IsMfaEnabled = personCredential.IsMfaEnabled,
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