using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
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
        root.RaiseCreateEvent(IdentityDomain.Events.PasswordCredentials.Created.Create(root.Id, userId));
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

    public bool IsPasswordInitiated => Password.IsInitiated;

    public bool IsPasswordResetStillValid => Password.IsInitiatingStillValid;

    public bool IsRegistrationVerified => Verification.IsVerified;

    public bool IsVerificationStillVerifying => Verification.IsStillVerifying;

    public bool IsVerificationVerifying => Verification.IsVerifying;

    public bool IsVerified => Verification.IsVerified;

    public LoginMonitor Login { get; private set; }

    public PasswordKeep Password { get; private set; } = PasswordKeep.Create().Value;

    public Optional<Registration> Registration { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public Verification Verification { get; private set; } = Verification.Create().Value;

    public static AggregateRootFactory<PasswordCredentialRoot> Rehydrate()
    {
        return (identifier, container, _) => new PasswordCredentialRoot(container.Resolve<IRecorder>(),
            container.Resolve<IIdentifierFactory>(), container.ResolveForPlatform<IConfigurationSettings>(),
            container.Resolve<IEmailAddressService>(), container.Resolve<ITokensService>(),
            container.Resolve<IPasswordHasherService>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
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
            && Password.IsInitiating)
        {
            return Error.RuleViolation(Resources.PasswordCredentialsRoot_PasswordInitiatedWithoutRegistration);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.PasswordCredentials.Created created:
            {
                UserId = created.UserId.ToId();
                Recorder.TraceDebug(null, "Password credential {Id} was created for {UserId}", Id, created.UserId);
                return Result.Ok;
            }

            case Events.PasswordCredentials.CredentialsChanged changed:
            {
                var set = Password.SetPassword(_passwordHasherService, changed.PasswordHash);
                if (!set.IsSuccessful)
                {
                    return set.Error;
                }

                Password = set.Value;
                Recorder.TraceDebug(null, "Password credential {Id} changed the credential", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.RegistrationChanged changed:
            {
                var registration = IdentityDomain.Registration.Create(changed.EmailAddress, changed.Name);
                if (!registration.IsSuccessful)
                {
                    return registration.Error;
                }

                Registration = registration.Value;
                Recorder.TraceDebug(null, "Password credential {Id} changed the registration details", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.PasswordVerified changed:
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

            case Events.PasswordCredentials.AccountLocked _:
            {
                Recorder.TraceDebug(null, "Password credential {Id} was locked", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.AccountUnlocked _:
            {
                Recorder.TraceDebug(null, "Password credential {Id} was unlocked", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.RegistrationVerificationCreated created:
            {
                Verification = Verification.Renew(created.Token);
                Recorder.TraceDebug(null, "Password credential {Id} verification has been renewed", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.RegistrationVerificationVerified _:
            {
                Verification = Verification.Verify();
                Recorder.TraceDebug(null, "Password credential {Id} has been verified", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.PasswordResetInitiated changed:
            {
                var reset = Password.InitiatePasswordReset(changed.Token);
                if (!reset.IsSuccessful)
                {
                    return reset.Error;
                }

                Password = reset.Value;
                Recorder.TraceDebug(null, "Password credential {Id} has initiated a password reset", Id);
                return Result.Ok;
            }

            case Events.PasswordCredentials.PasswordResetCompleted changed:
            {
                var reset = Password.ResetPassword(_passwordHasherService, changed.Token, changed.PasswordHash);
                if (!reset.IsSuccessful)
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

    public Result<Error> ConfirmPasswordReset(string token)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error))
        {
            return error;
        }

        var confirmed = Password.ConfirmReset(token);
        if (!confirmed.IsSuccessful)
        {
            return confirmed.Error;
        }

        Password = confirmed.Value;

        return Result.Ok;
    }

    public Result<Error> InitiatePasswordReset()
    {
        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_RegistrationUnverified);
        }

        var token = _tokensService.CreatePasswordResetToken();
        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.PasswordResetInitiated.Create(Id, token));
    }

    public Result<Error> InitiateRegistrationVerification()
    {
        if (IsVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_RegistrationVerified);
        }

        var token = _tokensService.CreateRegistrationVerificationToken();
        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.RegistrationVerificationCreated.Create(Id, token));
    }

    public Result<Error> ResetPassword(string token, string password)
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

        if (!IsPasswordInitiated)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsRoot_NoPassword);
        }

        if (password.IsInvalidParameter(pwd => _passwordHasherService.VerifyPassword(pwd, Password.PasswordHash),
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
        RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.PasswordResetCompleted.Create(Id, token, passwordHash));

        if (Login.HasJustUnlocked)
        {
            RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountUnlocked.Create(Id));
        }

        return Result.Ok;
    }

    public Result<Error> SetCredential(string password)
    {
        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, true),
                nameof(password), Resources.PasswordCredentialsRoot_InvalidPassword, out var error1))
        {
            return error1;
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.CredentialsChanged.Create(Id,
                _passwordHasherService.HashPassword(password)));
    }

    public Result<Error> SetRegistrationDetails(EmailAddress emailAddress, PersonDisplayName displayName)
    {
        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.RegistrationChanged.Create(Id, emailAddress, displayName));
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
        Verification = Verification.TestingOnly_ExpireToken();
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
        Verification = Verification.Renew(token);
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
        if (!verify.IsSuccessful)
        {
            return verify.Error;
        }

        var isVerified = verify.Value;
        var raised =
            RaiseChangeEvent(
                IdentityDomain.Events.PasswordCredentials.PasswordVerified.Create(Id, isVerified, auditAttempt));
        if (!raised.IsSuccessful)
        {
            return raised.Error;
        }

        if (Login.HasJustLocked)
        {
            var locked = RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountLocked.Create(Id));
            if (!locked.IsSuccessful)
            {
                return locked.Error;
            }
        }

        if (Login.HasJustUnlocked)
        {
            var unlocked = RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.AccountUnlocked.Create(Id));
            if (!unlocked.IsSuccessful)
            {
                return unlocked.Error;
            }
        }

        return isVerified;
    }

    public Result<Error> VerifyRegistration()
    {
        if (!IsVerificationStillVerifying)
        {
            return Error.PreconditionViolation(!IsVerificationVerifying
                ? Resources.PasswordCredentialsRoot_RegistrationNotVerifying
                : Resources.PasswordCredentialsRoot_RegistrationVerifyingExpired);
        }

        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.RegistrationVerificationVerified.Create(Id));
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