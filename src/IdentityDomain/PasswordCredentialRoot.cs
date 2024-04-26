using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public sealed class PasswordCredentialRoot : AggregateRootBase
{
    public const string CooldownPeriodInMinutesSettingName = "IdentityApi:PasswordCredential:CooldownPeriodInMinutes";
    public const string MaxFailedLoginsSettingName = "IdentityApi:PasswordCredential:MaxFailedLogins";
    private readonly IEmailAddressService _emailAddressService;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ITokensService _tokensService;

    public static Result<PasswordCredentialRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IConfigurationSettings settings, IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, Identifier userId)
    {
        var root = new PasswordCredentialRoot(recorder, idFactory, settings, emailAddressService, tokensService,
            passwordHasherService);
        root.RaiseCreateEvent(IdentityDomain.Events.PasswordCredentials.Created(root.Id, userId));
        return root;
    }

    private PasswordCredentialRoot(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService) :
        base(recorder, idFactory)
    {
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        Login = CreateLoginMonitor(settings);
    }

    private PasswordCredentialRoot(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        Login = CreateLoginMonitor(settings);
    }

    public bool IsLocked => Login.IsLocked;

    public bool IsPasswordResetInitiated => Password.IsResetInitiated;

    public bool IsPasswordResetStillValid => Password.IsResetStillValid;

    public bool IsPasswordSet => Password.HasPassword;

    public bool IsRegistrationVerified => VerificationKeep.IsVerified;

    public bool IsVerificationStillVerifying => VerificationKeep.IsStillVerifying;

    public bool IsVerificationVerifying => VerificationKeep.IsVerifying;

    public bool IsVerified => VerificationKeep.IsVerified;

    public LoginMonitor Login { get; private set; }

    public PasswordKeep Password { get; private set; } = PasswordKeep.Create().Value;

    public Optional<Registration> Registration { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public VerificationKeep VerificationKeep { get; private set; } = VerificationKeep.Create().Value;

    public static AggregateRootFactory<PasswordCredentialRoot> Rehydrate()
    {
        return (identifier, container, _) => new PasswordCredentialRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(),
            container.GetRequiredServiceForPlatform<IConfigurationSettings>(),
            container.GetRequiredService<IEmailAddressService>(), container.GetRequiredService<ITokensService>(),
            container.GetRequiredService<IPasswordHasherService>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (Registration.HasValue)
        {
            var isEmailUnique = _emailAddressService.EnsureUniqueAsync(Registration.Value.EmailAddress, UserId)
                .GetAwaiter().GetResult();
            if (!isEmailUnique)
            {
                return Error.RuleViolation(Resources.PasswordCredentialsRoot_EmailNotUnique);
            }
        }

        if (!Registration.HasValue
            && Password.IsResetInitiated)
        {
            return Error.RuleViolation(Resources.PasswordCredentialsRoot_PasswordInitiatedWithoutRegistration);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                UserId = created.UserId.ToId();
                Recorder.TraceDebug(null, "Password credential {Id} was created for {UserId}", Id, created.UserId);
                return Result.Ok;
            }

            case CredentialsChanged changed:
            {
                var set = Password.SetPassword(_passwordHasherService, changed.PasswordHash);
                if (set.IsFailure)
                {
                    return set.Error;
                }

                Password = set.Value;
                Recorder.TraceDebug(null, "Password credential {Id} changed the credential", Id);
                return Result.Ok;
            }

            case RegistrationChanged changed:
            {
                var registration = IdentityDomain.Registration.Create(changed.EmailAddress, changed.Name);
                if (registration.IsFailure)
                {
                    return registration.Error;
                }

                Registration = registration.Value;
                Recorder.TraceDebug(null, "Password credential {Id} changed the registration details", Id);
                return Result.Ok;
            }

            case PasswordVerified changed:
            {
                if (changed.AuditAttempt)
                {
                    Login = changed.IsVerified
                        ? Login.AttemptedSuccessfulLogin(changed.OccurredUtc)
                        : Login.AttemptedFailedLogin(changed.OccurredUtc);
                }

                Recorder.TraceDebug(null, "Password credential {Id} verified and audited", Id);
                return Result.Ok;
            }

            case AccountLocked _:
            {
                Recorder.TraceDebug(null, "Password credential {Id} was locked", Id);
                return Result.Ok;
            }

            case AccountUnlocked _:
            {
                Recorder.TraceDebug(null, "Password credential {Id} was unlocked", Id);
                return Result.Ok;
            }

            case RegistrationVerificationCreated created:
            {
                VerificationKeep = VerificationKeep.Renew(created.Token);
                Recorder.TraceDebug(null, "Password credential {Id} verification has been renewed", Id);
                return Result.Ok;
            }

            case RegistrationVerificationVerified _:
            {
                VerificationKeep = VerificationKeep.Verify();
                Recorder.TraceDebug(null, "Password credential {Id} has been verified", Id);
                return Result.Ok;
            }

            case PasswordResetInitiated changed:
            {
                var reset = Password.InitiatePasswordReset(changed.Token);
                if (reset.IsFailure)
                {
                    return reset.Error;
                }

                Password = reset.Value;
                Recorder.TraceDebug(null, "Password credential {Id} has initiated a password reset", Id);
                return Result.Ok;
            }

            case PasswordResetCompleted changed:
            {
                var reset = Password.CompletePasswordReset(_passwordHasherService, changed.Token, changed.PasswordHash);
                if (reset.IsFailure)
                {
                    return reset.Error;
                }

                Password = reset.Value;
                Login = Login.Unlock(changed.OccurredUtc);
                Recorder.TraceDebug(null, "Credentials {Id} password reset has been completed", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> CompletePasswordReset(string token, string password)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, false),
                nameof(password), Resources.PasswordCredentialsRoot_InvalidPassword, out var error2))
        {
            return error2;
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_NoPassword);
        }

        if (password.IsInvalidParameter(pwd => !_passwordHasherService.VerifyPassword(pwd, Password.PasswordHash),
                nameof(password), Resources.PasswordCredentialsRoot_DuplicatePassword, out var error3))
        {
            return error3;
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_RegistrationUnverified);
        }

        if (!IsPasswordResetStillValid)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_PasswordResetTokenExpired);
        }

        var passwordHash = _passwordHasherService.HashPassword(password);
        var completed = RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.PasswordResetCompleted(Id, token, passwordHash));
        if (completed.IsFailure)
        {
            return completed.Error;
        }

        if (Login.HasJustUnlocked)
        {
            return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountUnlocked(Id));
        }

        return Result.Ok;
    }

    public Result<Error> InitiatePasswordReset()
    {
        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_RegistrationUnverified);
        }

        var token = _tokensService.CreatePasswordResetToken();
        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.PasswordResetInitiated(Id, token));
    }

    public Result<Error> InitiateRegistrationVerification()
    {
        if (IsVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_RegistrationVerified);
        }

        var token = _tokensService.CreateRegistrationVerificationToken();
        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.RegistrationVerificationCreated(Id, token));
    }

    public Result<Error> SetPasswordCredential(string password)
    {
        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, true),
                nameof(password), Resources.PasswordCredentialsRoot_InvalidPassword, out var error1))
        {
            return error1;
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.CredentialsChanged(Id,
                _passwordHasherService.HashPassword(password)));
    }

    public Result<Error> SetRegistrationDetails(EmailAddress emailAddress, PersonDisplayName displayName)
    {
        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.RegistrationChanged(Id, emailAddress, displayName));
    }

#if TESTINGONLY
    public void TestingOnly_ExpirePasswordResetVerification()
    {
        Password = Password.TestingOnly_ExpireToken();
    }
#endif

#if TESTINGONLY
    public void TestingOnly_ExpireRegistrationVerification()
    {
        VerificationKeep = VerificationKeep.TestingOnly_ExpireToken();
    }
#endif

#if TESTINGONLY
    public void TestingOnly_LockAccount(string wrongPassword)
    {
        Repeat.Times(() => { VerifyPassword(wrongPassword); },
            Validations.Credentials.Login.DefaultMaxFailedPasswordAttempts);
    }
#endif

#if TESTINGONLY
    public void TestingOnly_RenewVerification(string token)
    {
        VerificationKeep = VerificationKeep.Renew(token);
    }
#endif

#if TESTINGONLY
    public void TestingOnly_ResetLoginCooldownPeriod()
    {
        Login = Login.TestingOnly_ResetCooldownPeriod();
    }
#endif

#if TESTINGONLY
    public void TestingOnly_Unregister()
    {
        Registration = Optional<Registration>.None;
    }
#endif

    public Result<bool, Error> VerifyPassword(string password, bool auditAttempt = true)
    {
        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, false),
                nameof(password), Resources.PasswordCredentialsRoot_InvalidPassword, out var error1))
        {
            return error1;
        }

        var verify = Password.Verify(_passwordHasherService, password);
        if (verify.IsFailure)
        {
            return verify.Error;
        }

        var isVerified = verify.Value;
        var raised =
            RaiseChangeEvent(
                IdentityDomain.Events.PasswordCredentials.PasswordVerified(Id, isVerified, auditAttempt));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        if (Login.HasJustLocked)
        {
            var locked = RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountLocked(Id));
            if (locked.IsFailure)
            {
                return locked.Error;
            }
        }

        if (Login.HasJustUnlocked)
        {
            var unlocked = RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountUnlocked(Id));
            if (unlocked.IsFailure)
            {
                return unlocked.Error;
            }
        }

        return isVerified;
    }

    public Result<Error> VerifyPasswordReset(string token)
    {
        var verified = Password.VerifyReset(token);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        Password = verified.Value;

        return Result.Ok;
    }

    public Result<Error> VerifyRegistration()
    {
        if (!IsVerificationStillVerifying)
        {
            return Error.PreconditionViolation(!IsVerificationVerifying
                ? Resources.PasswordCredentialsRoot_RegistrationNotVerifying
                : Resources.PasswordCredentialsRoot_RegistrationVerifyingExpired);
        }

        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.RegistrationVerificationVerified(Id));
    }

    private static LoginMonitor CreateLoginMonitor(IConfigurationSettings settings)
    {
        return LoginMonitor.Create(
            (int)settings.Platform.GetNumber(MaxFailedLoginsSettingName,
                Validations.Credentials.Login.DefaultMaxFailedPasswordAttempts),
            TimeSpan.FromMinutes(settings.Platform.GetNumber(CooldownPeriodInMinutesSettingName,
                Validations.Credentials.Login.DefaultCooldownPeriodMinutes)
            )).Value;
    }
}