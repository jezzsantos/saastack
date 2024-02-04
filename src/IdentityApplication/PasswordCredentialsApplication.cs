using Application.Common;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication;

public class PasswordCredentialsApplication : IPasswordCredentialsApplication
{
#if TESTINGONLY
    private const double MinAuthenticateDelayInSecs = 0;
    private const double MaxAuthenticateDelayInSecs = 0;
#else
        private const double MinAuthenticateDelayInSecs = 1.5;
        private const double MaxAuthenticateDelayInSecs = 4.0;
#endif
    private readonly IEndUsersService _endUsersService;
    private readonly INotificationsService _notificationsService;
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

    public PasswordCredentialsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, INotificationsService notificationsService, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPasswordCredentialsRepository repository) : this(recorder,
        identifierFactory, endUsersService, notificationsService, settings, emailAddressService, tokensService,
        passwordHasherService, authTokensService, websiteUiService, repository, new DelayGenerator())
    {
        _recorder = recorder;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    private PasswordCredentialsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, INotificationsService notificationsService, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, IAuthTokensService authTokensService,
        IWebsiteUiService websiteUiService,
        IPasswordCredentialsRepository repository,
        IDelayGenerator delayGenerator)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _endUsersService = endUsersService;
        _notificationsService = notificationsService;
        _settings = settings;
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        _authTokensService = authTokensService;
        _websiteUiService = websiteUiService;
        _repository = repository;
        _delayGenerator = delayGenerator;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string username,
        string password, CancellationToken cancellationToken)
    {
        await DelayForRandomPeriodAsync(MinAuthenticateDelayInSecs, MaxAuthenticateDelayInSecs, cancellationToken);

        var fetched = await _repository.FindCredentialsByUsernameAsync(username, cancellationToken);
        if (!fetched.IsSuccessful)
        {
            return Error.NotAuthenticated();
        }

        if (!fetched.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var credentials = fetched.Value.Value;
        var retrieved = await _endUsersService.GetMembershipsAsync(context, credentials.UserId, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return Error.NotAuthenticated();
        }

        var user = retrieved.Value;
        if (user.Status != EndUserStatus.Registered)
        {
            return Error.NotAuthenticated();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(context.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate a password with a suspended account", user.Id);
            return Error.EntityExists(Resources.PasswordCredentialsApplication_AccountSuspended);
        }

        if (credentials.IsLocked)
        {
            _recorder.AuditAgainst(context.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_AccountLocked,
                "User {Id} tried to authenticate a password with a locked account", user.Id);
            return Error.EntityExists(Resources.PasswordCredentialsApplication_AccountLocked);
        }

        var verifyPassword = await VerifyPasswordAsync();
        if (!verifyPassword.IsSuccessful)
        {
            return verifyPassword.Error;
        }

        var isVerified = verifyPassword.Value;
        if (!isVerified)
        {
            _recorder.AuditAgainst(context.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_InvalidCredentials,
                "User {Id} failed to authenticate with an invalid password", user.Id);
            return Error.NotAuthenticated();
        }

        if (!credentials.IsVerified)
        {
            _recorder.AuditAgainst(context.ToCall(), user.Id,
                Audits.PasswordCredentialsApplication_Authenticate_BeforeVerified,
                "User {Id} tried to authenticate with a password before verifying their account", user.Id);
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_RegistrationNotVerified);
        }

        _recorder.AuditAgainst(context.ToCall(), user.Id,
            Audits.PasswordCredentialsApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with a password", user.Id);

        var issued = await _authTokensService.IssueTokensAsync(context, user, cancellationToken);
        if (!issued.IsSuccessful)
        {
            return issued.Error;
        }

        var tokens = issued.Value;
        return new Result<AuthenticateTokens, Error>(new AuthenticateTokens
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresOn = tokens.ExpiresOn,
            UserId = user.Id
        });

        async Task<Result<bool, Error>> VerifyPasswordAsync()
        {
            var verify = credentials.VerifyPassword(password);
            if (!verify.IsSuccessful)
            {
                return verify.Error;
            }

            var saved1 = await _repository.SaveAsync(credentials, cancellationToken);
            if (!saved1.IsSuccessful)
            {
                return saved1.Error;
            }

            _recorder.TraceInformation(context.ToCall(), "Credentials were verified for {Id}", saved1.Value.Id);

            return verify.Value;
        }
    }

    public async Task<Result<PasswordCredential, Error>> RegisterPersonAsync(ICallerContext context, string firstName,
        string lastName, string emailAddress, string password, string? timezone, string? countryCode,
        bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        var registered = await _endUsersService.RegisterPersonAsync(context, emailAddress, firstName, lastName,
            timezone, countryCode, termsAndConditionsAccepted, cancellationToken);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        return await RegisterPersonInternalAsync(context, password, registered.Value, cancellationToken);
    }

#if TESTINGONLY
    public async Task<Result<PasswordCredentialConfirmation, Error>> GetPersonRegistrationConfirmationAsync(
        ICallerContext context, string userId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByUserIdAsync(userId.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        return new PasswordCredentialConfirmation
        {
            Token = credential.Verification.Token,
            Url = _websiteUiService.ConstructPasswordRegistrationConfirmationPageUrl(credential.Verification.Token)
        };
    }
#endif

    public async Task<Result<Error>> ConfirmPersonRegistrationAsync(ICallerContext context, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByTokenAsync(token, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        var verified = credential.VerifyRegistration();
        if (!verified.IsSuccessful)
        {
            return verified.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Password credentials for {UserId} have been verified",
            credential.UserId);
        _recorder.TrackUsage(context.ToCall(),
            UsageConstants.Events.UsageScenarios.PersonRegistrationConfirmed,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId }
            });

        return Result.Ok;
    }

    private async Task<Result<PasswordCredential, Error>> RegisterPersonInternalAsync(ICallerContext context,
        string password, RegisteredEndUser user, CancellationToken cancellationToken)
    {
        var fetched = await _repository.FindCredentialsByUserIdAsync(user.Id.ToId(), cancellationToken);
        if (!fetched.IsSuccessful)
        {
            return fetched.Error;
        }

        if (fetched.Value.HasValue)
        {
            return fetched.Value.Value.ToCredential(user);
        }

        var created = PasswordCredentialRoot.Create(_recorder, _identifierFactory, _settings, _emailAddressService,
            _tokensService, _passwordHasherService, user.Id.ToId());
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var emailAddress = EmailAddress.Create(user.Profile!.EmailAddress!);
        if (!emailAddress.IsSuccessful)
        {
            return emailAddress.Error;
        }

        var name = PersonDisplayName.Create(user.Profile.DisplayName);
        if (!name.IsSuccessful)
        {
            return name.Error;
        }

        var credentials = created.Value;
        var credentialed = credentials.SetCredential(password);
        if (!credentialed.IsSuccessful)
        {
            return credentialed.Error;
        }

        var registered = credentials.SetRegistrationDetails(emailAddress.Value, name.Value);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var initiated = credentials.InitiateRegistrationVerification();
        if (!initiated.IsSuccessful)
        {
            return initiated.Error;
        }

        var updated = await _repository.SaveAsync(credentials, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        await _notificationsService.NotifyPasswordRegistrationConfirmationAsync(context,
            updated.Value.Registration.Value.EmailAddress,
            updated.Value.Registration.Value.Name, updated.Value.Verification.Token, cancellationToken);

        _recorder.TraceInformation(context.ToCall(), "Password credentials created for {UserId}", updated.Value.UserId);

        return updated.Value.ToCredential(user);
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
    public static PasswordCredential ToCredential(this PasswordCredentialRoot credential, RegisteredEndUser user)
    {
        return new PasswordCredential
        {
            Id = credential.Id,
            User = user
        };
    }
}