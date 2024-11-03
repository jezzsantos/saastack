using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public delegate Task<Result<Error>> NotifyChallenged(MfaAuthenticator associatedAuthenticator);

public sealed class PasswordCredentialRoot : AggregateRootBase
{
    private const string CooldownPeriodInMinutesSettingName = "IdentityApi:PasswordCredential:CooldownPeriodInMinutes";
    private const string MaxFailedLoginsSettingName = "IdentityApi:PasswordCredential:MaxFailedLogins";
    // EXTEND: Change default MFA options for all users
    private static readonly MfaOptions DefaultMfaOptions = MfaOptions.Create(false, true).Value;
    private readonly IEmailAddressService _emailAddressService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMfaService _mfaService;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ITokensService _tokensService;

#pragma warning disable SAASDDD012
    public static Result<PasswordCredentialRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
#pragma warning restore SAASDDD012
        IConfigurationSettings settings, IEmailAddressService emailAddressService, ITokensService tokensService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService,
        Identifier userId)
    {
        return Create(recorder, idFactory, settings, emailAddressService, tokensService, encryptionService,
            passwordHasherService, mfaService, userId, DefaultMfaOptions);
    }

    internal static Result<PasswordCredentialRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IConfigurationSettings settings, IEmailAddressService emailAddressService, ITokensService tokensService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService,
        Identifier userId, MfaOptions mfaOptions)
    {
        var root = new PasswordCredentialRoot(recorder, idFactory, settings, emailAddressService, tokensService,
            encryptionService, passwordHasherService, mfaService);
        root.RaiseCreateEvent(IdentityDomain.Events.PasswordCredentials.Created(root.Id, userId, mfaOptions));
        return root;
    }

    private PasswordCredentialRoot(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService) :
        base(recorder, idFactory)
    {
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _encryptionService = encryptionService;
        _passwordHasherService = passwordHasherService;
        _mfaService = mfaService;
        Login = CreateLoginMonitor(settings);
    }

    private PasswordCredentialRoot(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IEmailAddressService emailAddressService, ITokensService tokensService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _emailAddressService = emailAddressService;
        _tokensService = tokensService;
        _encryptionService = encryptionService;
        _passwordHasherService = passwordHasherService;
        _mfaService = mfaService;
        Login = CreateLoginMonitor(settings);
    }

    public bool IsLocked => Login.IsLocked;

    public bool IsMfaEnabled => MfaOptions.IsEnabled;

    public bool IsPasswordResetInitiated => Password.IsResetInitiated;

    public bool IsPasswordResetStillValid => Password.IsResetStillValid;

    public bool IsPasswordSet => Password.HasPassword;

    public bool IsRegistrationVerified => VerificationKeep.IsVerified;

    public bool IsVerificationStillVerifying => VerificationKeep.IsStillVerifying;

    public bool IsVerificationVerifying => VerificationKeep.IsVerifying;

    public bool IsVerified => VerificationKeep.IsVerified;

    public LoginMonitor Login { get; private set; }

    public MfaAuthenticators MfaAuthenticators { get; } = new();

    public MfaOptions MfaOptions { get; private set; } = MfaOptions.Default;

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
            container.GetRequiredService<IEncryptionService>(),
            container.GetRequiredService<IPasswordHasherService>(),
            container.GetRequiredService<IMfaService>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        var mfaAuthenticators = MfaAuthenticators.EnsureInvariants();
        if (mfaAuthenticators.IsFailure)
        {
            return mfaAuthenticators.Error;
        }

        if (Registration.HasValue)
        {
            var isEmailUnique = _emailAddressService.EnsureUniqueAsync(Registration.Value.EmailAddress, UserId)
                .GetAwaiter().GetResult();
            if (!isEmailUnique)
            {
                return Error.RuleViolation(Resources.PasswordCredentialRoot_EmailNotUnique);
            }
        }

        if (!Registration.HasValue
            && Password.IsResetInitiated)
        {
            return Error.RuleViolation(Resources.PasswordCredentialRoot_PasswordInitiatedWithoutRegistration);
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
                var mfaOptions =
                    MfaOptions.Create(created.IsMfaEnabled, created.MfaCanBeDisabled);
                if (mfaOptions.IsFailure)
                {
                    return mfaOptions.Error;
                }

                MfaOptions = mfaOptions.Value;
                Recorder.TraceDebug(null, "Password credential {Id} was created for {UserId}, with MFA {IsMfaEnabled}",
                    Id,
                    created.UserId, created.IsMfaEnabled);
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

            case MfaOptionsChanged changed:
            {
                var options =
                    MfaOptions.Create(changed.IsEnabled, changed.CanBeDisabled);
                if (options.IsFailure)
                {
                    return options.Error;
                }

                MfaOptions = options.Value;
                Recorder.TraceDebug(null,
                    "Password credential {Id} changed MFA options, enabled {IsEnabled}, canBeDisabled {CanBeDisabled}",
                    Id, changed.IsEnabled, changed.CanBeDisabled);
                return Result.Ok;
            }

            case MfaStateReset reset:
            {
                var options =
                    MfaOptions.Create(reset.IsEnabled, reset.CanBeDisabled);
                if (options.IsFailure)
                {
                    return options.Error;
                }

                MfaOptions = options.Value;
                Recorder.TraceDebug(null,
                    "Password credential {Id} reset MFA state, enabled {IsEnabled}, canBeDisabled {CanBeDisabled}",
                    Id, reset.IsEnabled, reset.CanBeDisabled);
                return Result.Ok;
            }

            case MfaAuthenticationInitiated initiated:
            {
                var options = MfaOptions.Create(true, MfaOptions.CanBeDisabled, initiated.AuthenticationToken,
                    initiated.AuthenticationExpiresAt);
                if (options.IsFailure)
                {
                    return options.Error;
                }

                MfaOptions = options.Value;
                Recorder.TraceDebug(null,
                    "Password credential {Id} initiated MFA authentication",
                    Id);
                return Result.Ok;
            }

            case MfaAuthenticatorAdded added:
            {
                var authenticator = RaiseEventToChildEntity(isReconstituting, added, idFactory =>
                        MfaAuthenticator.Create(Recorder, idFactory, _encryptionService,
                            _mfaService,
                            RaiseChangeEvent),
                    e => e.AuthenticatorId!);
                if (authenticator.IsFailure)
                {
                    return authenticator.Error;
                }

                MfaAuthenticators.Add(authenticator.Value);
                Recorder.TraceDebug(null, "Password credential {Id} added authenticator of type {Type}",
                    Id, added.Type);
                return Result.Ok;
            }

            case MfaAuthenticatorRemoved removed:
            {
                MfaAuthenticators.Remove(removed.AuthenticatorId.ToId());
                Recorder.TraceDebug(null, "CPassword credential {Id} has had authenticator {AuthenticatorId} removed",
                    Id, removed.AuthenticatorId);
                return Result.Ok;
            }

            case MfaAuthenticatorAssociated associated:
            {
                var authenticator = MfaAuthenticators.FindById(associated.AuthenticatorId.ToId());
                if (!authenticator.HasValue)
                {
                    return Error.RuleViolation(Resources.PasswordCredentialRoot_NoAuthenticator);
                }

                var forwarded = RaiseEventToChildEntity(associated, authenticator.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "Password credential {Id} is associating authenticator of type {Type}",
                    Id, associated.Type);
                return Result.Ok;
            }

            case MfaAuthenticatorChallenged challenged:
            {
                var authenticator = MfaAuthenticators.FindById(challenged.AuthenticatorId.ToId());
                if (!authenticator.HasValue)
                {
                    return Error.RuleViolation(Resources.PasswordCredentialRoot_NoAuthenticator);
                }

                var forwarded = RaiseEventToChildEntity(challenged, authenticator.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "Password credential {Id} has challenged authenticator of type {Type}",
                    Id, challenged.Type);
                return Result.Ok;
            }

            case MfaAuthenticatorConfirmed confirmed:
            {
                var authenticator = MfaAuthenticators.FindById(confirmed.AuthenticatorId.ToId());
                if (!authenticator.HasValue)
                {
                    return Error.RuleViolation(Resources.PasswordCredentialRoot_NoAuthenticator);
                }

                var forwarded = RaiseEventToChildEntity(confirmed, authenticator.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null,
                    "Password credential {Id} has associated authenticator {AuthenticatorId} of type {Type}",
                    Id, confirmed.AuthenticatorId, confirmed.Type);
                return Result.Ok;
            }

            case MfaAuthenticatorVerified verified:
            {
                var authenticator = MfaAuthenticators.FindById(verified.AuthenticatorId.ToId());
                if (!authenticator.HasValue)
                {
                    return Error.RuleViolation(Resources.PasswordCredentialRoot_NoAuthenticator);
                }

                var forwarded = RaiseEventToChildEntity(verified, authenticator.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null,
                    "Password credential {Id} has verified authenticator {AuthenticatorId} of type {Type}",
                    Id, verified.AuthenticatorId, verified.Type);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public async Task<Result<MfaAuthenticator, Error>> AssociateMfaAuthenticatorAsync(MfaCaller caller,
        MfaAuthenticatorType type, Optional<PhoneNumber> oobPhoneNumber,
        Optional<EmailAddress> oobEmailAddress, Optional<EmailAddress> otpUsername, NotifyChallenged onChallenged)
    {
        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticated = MfaOptions.Authenticate(caller);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        if (type is MfaAuthenticatorType.None or MfaAuthenticatorType.RecoveryCodes)
        {
            return Error.RuleViolation(Resources
                .PasswordCredentialRoot_AssociateMfaAuthenticator_InvalidType);
        }

        if (!caller.IsAuthenticated)
        {
            if (MfaAuthenticators.HasAnyConfirmedPlusRecoveryCodes)
            {
                return Error.PreconditionViolation(Resources
                    .PasswordCredentialRoot_AssociateMfaAuthenticator_MustChallenge);
            }
        }

        var authenticator = MfaAuthenticators.FindByType(type);
        if (authenticator is { HasValue: true, Value.HasBeenConfirmed: true })
        {
            return Error.PreconditionViolation(Resources
                .PasswordCredentialRoot_AssociateMfaAuthenticator_AlreadyAssociated);
        }

        var recoveryCodesAuthenticator = MfaAuthenticators.FindRecoveryCodes();
        if (!recoveryCodesAuthenticator.HasValue)
        {
            var recoveryCodesCompleted = AddRecoveryCodes();
            if (recoveryCodesCompleted.IsFailure)
            {
                return recoveryCodesCompleted.Error;
            }
        }

        if (!authenticator.HasValue)
        {
            var added =
                RaiseChangeEvent(
                    IdentityDomain.Events.PasswordCredentials.MfaAuthenticatorAdded(Id, UserId, type, true));
            if (added.IsFailure)
            {
                return added.Error;
            }

            authenticator = MfaAuthenticators.FindByType(type).Value;
        }

        var associated =
            authenticator.Value.Associate(oobPhoneNumber, oobEmailAddress, otpUsername, Optional<string>.None);
        if (associated.IsFailure)
        {
            return associated.Error;
        }

        // Send challenge to user
        authenticator = MfaAuthenticators.FindByType(type).Value;
        var handled = await onChallenged(authenticator.Value);
        if (handled.IsFailure)
        {
            return handled.Error;
        }

        return authenticator.Value;

        Result<Error> AddRecoveryCodes()
        {
            var recoveryAdded =
                RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.MfaAuthenticatorAdded(Id, UserId,
                    MfaAuthenticatorType.RecoveryCodes, true));
            if (recoveryAdded.IsFailure)
            {
                return recoveryAdded.Error;
            }

            recoveryCodesAuthenticator = MfaAuthenticators.FindRecoveryCodes();
            var recoveryCodes = MfaAuthenticators.GenerateRecoveryCodes();
            var recoveryAssociated = recoveryCodesAuthenticator.Value.Associate(Optional<PhoneNumber>.None,
                Optional<EmailAddress>.None, Optional<EmailAddress>.None, recoveryCodes);
            if (recoveryAssociated.IsFailure)
            {
                return recoveryAssociated.Error;
            }

            return recoveryCodesAuthenticator.Value.ConfirmAssociation(Optional<string>.None,
                Optional<string>.None);
        }
    }

    public async Task<Result<MfaAuthenticator, Error>> ChallengeMfaAuthenticatorAsync(MfaCaller caller,
        Identifier authenticatorId, NotifyChallenged onChallenged)
    {
        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticated = MfaOptions.Authenticate(caller);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var authenticator = MfaAuthenticators.FindById(authenticatorId);
        if (!authenticator.HasValue)
        {
            return Error.EntityNotFound();
        }

        var challenged = authenticator.Value.Challenge();
        if (challenged.IsFailure)
        {
            return challenged.Error;
        }

        // Send challenge to user
        var handled = await onChallenged(authenticator.Value);
        if (handled.IsFailure)
        {
            return handled.Error;
        }

        return authenticator.Value;
    }

    public Result<Error> ChangeMfaEnabled(Identifier modifierId, bool isEnabled)
    {
        if (!IsOwner(modifierId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (MfaOptions.IsEnabled == isEnabled)
        {
            return Result.Ok;
        }

        var changed = MfaOptions.Enable(isEnabled);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        if (!isEnabled)
        {
            var deleted = DeleteAllMfaAuthenticators();
            if (deleted.IsFailure)
            {
                return deleted.Error;
            }
        }

        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.MfaOptionsChanged(Id, UserId, changed.Value));
    }

    public Result<Error> CompletePasswordReset(string token, string password)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, false),
                nameof(password), Resources.PasswordCredentialRoot_InvalidPassword, out var error2))
        {
            return error2;
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (password.IsInvalidParameter(pwd => !_passwordHasherService.VerifyPassword(pwd, Password.PasswordHash),
                nameof(password), Resources.PasswordCredentialRoot_DuplicatePassword, out var error3))
        {
            return error3;
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordResetStillValid)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_PasswordResetTokenExpired);
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

    public Result<Error> ConfirmMfaAuthenticatorAssociation(MfaCaller caller,
        MfaAuthenticatorType type, string? oobCode, string confirmationCode)
    {
        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.RuleViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticated = MfaOptions.Authenticate(caller);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        if (type is MfaAuthenticatorType.None or MfaAuthenticatorType.RecoveryCodes)
        {
            return Error.RuleViolation(Resources
                .PasswordCredentialRoot_AssociateMfaAuthenticator_InvalidType);
        }

        var authenticator = MfaAuthenticators.FindByType(type);
        if (!authenticator.HasValue)
        {
            return Error.PreconditionViolation(Resources
                .PasswordCredentialRoot_CompleteMfaAuthenticatorAssociation_NotFound);
        }

        return authenticator.Value.ConfirmAssociation(oobCode, confirmationCode);
    }

    public Result<MfaAuthenticator, Error> DisassociateMfaAuthenticator(MfaCaller caller,
        Identifier authenticatorId)
    {
        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.RuleViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticator = MfaAuthenticators.FindById(authenticatorId);
        if (!authenticator.HasValue)
        {
            return Error.EntityNotFound();
        }

        var recoveryCodesAuthenticator = MfaAuthenticators.FindRecoveryCodes();
        if (recoveryCodesAuthenticator.HasValue
            && recoveryCodesAuthenticator.Value.Id == authenticatorId)
        {
            return Error.RuleViolation(Resources
                .PasswordCredentialRoot_DisassociateMfaAuthenticator_RecoveryCodesCannotBeDeleted);
        }

        var authenticatorDeleted = RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.MfaAuthenticatorRemoved(Id, UserId, authenticator));
        if (authenticatorDeleted.IsFailure)
        {
            return authenticatorDeleted.Error;
        }

        if (MfaAuthenticators.HasOnlyRecoveryCodes)
        {
            var recoveryDeleted = RaiseChangeEvent(
                IdentityDomain.Events.PasswordCredentials.MfaAuthenticatorRemoved(Id, UserId,
                    recoveryCodesAuthenticator.Value));
            if (recoveryDeleted.IsFailure)
            {
                return recoveryDeleted.Error;
            }
        }

        return authenticator.Value;
    }

    public Result<string, Error> InitiateMfaAuthentication()
    {
        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        var initiated = MfaOptions.InitiateAuthentication(_tokensService);
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var raised =
            RaiseChangeEvent(
                IdentityDomain.Events.PasswordCredentials.MfaAuthenticationInitiated(Id, UserId, initiated.Value));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        return MfaOptions.AuthenticationToken.Value;
    }

    public Result<Error> InitiatePasswordReset()
    {
        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        var token = _tokensService.CreatePasswordResetToken();
        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.PasswordResetInitiated(Id, token));
    }

    public Result<Error> InitiateRegistrationVerification()
    {
        if (IsVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationVerified);
        }

        var token = _tokensService.CreateRegistrationVerificationToken();
        return RaiseChangeEvent(
            IdentityDomain.Events.PasswordCredentials.RegistrationVerificationCreated(Id, token));
    }

    public Result<Error> ResetMfa(Roles resetterRoles)
    {
        if (!IsOperations(resetterRoles))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOperator);
        }

        if (!IsRegistrationVerified)
        {
            return Result.Ok;
        }

        if (!IsPasswordSet)
        {
            return Result.Ok;
        }

        var deleted = DeleteAllMfaAuthenticators();
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.MfaStateReset(Id, UserId, DefaultMfaOptions));
    }

    public Result<Error> SetPasswordCredential(string password)
    {
        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, true),
                nameof(password), Resources.PasswordCredentialRoot_InvalidPassword, out var error1))
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

    public Result<Error> VerifyMfaAuthenticator(MfaCaller caller,
        MfaAuthenticatorType type, string? oobCode, string confirmationCode)
    {
        if (caller.IsAuthenticated)
        {
            return Error.ForbiddenAccess();
        }

        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticated = MfaOptions.Authenticate(caller);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        if (type is MfaAuthenticatorType.None)
        {
            return Error.RuleViolation(Resources
                .PasswordCredentialRoot_AssociateMfaAuthenticator_InvalidType);
        }

        var authenticator = MfaAuthenticators.FindByType(type);
        if (!authenticator.HasValue)
        {
            return Error.PreconditionViolation(Resources
                .PasswordCredentialRoot_CompleteMfaAuthenticatorAssociation_NotFound);
        }

        return authenticator.Value.Verify(oobCode, confirmationCode);
    }

    public Result<bool, Error> VerifyPassword(string password, bool auditAttempt = true)
    {
        if (password.IsInvalidParameter(pwd => _passwordHasherService.ValidatePassword(pwd, false),
                nameof(password), Resources.PasswordCredentialRoot_InvalidPassword, out var error1))
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
                ? Resources.PasswordCredentialRoot_RegistrationNotVerifying
                : Resources.PasswordCredentialRoot_RegistrationVerifyingExpired);
        }

        return RaiseChangeEvent(IdentityDomain.Events.PasswordCredentials.RegistrationVerificationVerified(Id));
    }

    public Result<Error> ViewMfaAuthenticators(MfaCaller caller)
    {
        if (!IsOwner(caller.CallerId))
        {
            return Error.RoleViolation(Resources.PasswordCredentialRoot_NotOwner);
        }

        if (!IsRegistrationVerified)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_RegistrationUnverified);
        }

        if (!IsPasswordSet)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_NoPassword);
        }

        if (!IsMfaEnabled)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialRoot_MfaNotEnabled);
        }

        var authenticated = MfaOptions.Authenticate(caller);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        return Result.Ok;
    }

    private Result<Error> DeleteAllMfaAuthenticators()
    {
        var authenticators = MfaAuthenticators.WithoutRecoveryCodes();
        foreach (var authenticator in authenticators)
        {
            var caller = MfaCaller.Create(UserId, null);
            if (caller.IsFailure)
            {
                return caller.Error;
            }

            var disassociated = DisassociateMfaAuthenticator(caller.Value, authenticator.Id);
            if (disassociated.IsFailure)
            {
                return disassociated.Error;
            }
        }

        return Result.Ok;
    }

    private bool IsOwner(Identifier userId)
    {
        return UserId == userId;
    }

    private static bool IsOperations(Roles roles)
    {
        return roles.HasRole(PlatformRoles.Operations);
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