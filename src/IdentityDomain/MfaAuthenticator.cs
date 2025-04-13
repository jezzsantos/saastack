using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.PersonCredentials;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public sealed class MfaAuthenticator : EntityBase
{
    private const string RecoveryCodeDelimiter = ";";
    private const string TotpTimeStepDelimiter = ";";
    private readonly IEncryptionService _encryptionService;
    private readonly IMfaService _mfaService;

    public static Result<MfaAuthenticator, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IEncryptionService encryptionService, IMfaService mfaService,
        RootEventHandler rootEventHandler)
    {
        return new MfaAuthenticator(recorder, idFactory, mfaService, encryptionService,
            rootEventHandler);
    }

    private MfaAuthenticator(IRecorder recorder, IIdentifierFactory idFactory, IMfaService mfaService,
        IEncryptionService encryptionService,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
        _mfaService = mfaService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    ///     The barcode used to program the Authenticator App automatically
    /// </summary>
    public Optional<string> BarCodeUri { get; private set; } = Optional<string>.None;

    public bool HasBeenConfirmed => State is MfaAuthenticatorState.Confirmed or MfaAuthenticatorState.Verified
        or MfaAuthenticatorState.Challenged;

    public bool IsActive { get; private set; }

    public bool IsAssociated => State is MfaAuthenticatorState.Associated;

    public bool IsChallenged => State == MfaAuthenticatorState.Challenged;

    public bool IsConfigurable => State is MfaAuthenticatorState.Created or MfaAuthenticatorState.Associated;

    /// <summary>
    ///     The destination of the OOB channel (e.g. the phone number or email address)
    /// </summary>
    public Optional<string> OobChannelValue { get; private set; } = Optional<string>.None;

    /// <summary>
    ///     A random code that is used in challenge-verification of the OOB channel.
    /// </summary>
    public Optional<string> OobCode { get; private set; } = Optional<string>.None;

    public Optional<Identifier> RootId { get; private set; } = Optional<Identifier>.None;

    /// <summary>
    ///     The secret that is used to compare in the challenge-verification
    /// </summary>
    /// <remarks>
    ///     In the case of RecoveryCodes, this is the salted-hashed remaining codes to be used up.
    ///     In the case of OTP, this is the raw secret that was used to seed the TOTP code.
    ///     In the case of OOB, this is the salted-hashed code that was last used to compare with the value sent over the OOB
    ///     channel.
    /// </remarks>
    public Optional<string> Secret { get; private set; }

    public MfaAuthenticatorState State { get; private set; }

    public MfaAuthenticatorType Type { get; private set; }

    public Optional<Identifier> UserId { get; private set; }

    /// <summary>
    ///     Any state that needs to be persisted between subsequent challenges
    /// </summary>
    public Optional<string> VerifiedState { get; private set; } = Optional<string>.None;

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case MfaAuthenticatorAdded added:
            {
                RootId = added.RootId.ToId();
                UserId = added.UserId.ToId();
                IsActive = false;
                State = MfaAuthenticatorState.Created;
                Type = added.Type;
                OobChannelValue = Optional<string>.None;
                OobCode = Optional<string>.None;
                BarCodeUri = Optional<string>.None;
                Secret = Optional<string>.None;
                return Result.Ok;
            }

            case MfaAuthenticatorAssociated associated:
            {
                State = MfaAuthenticatorState.Associated;
                OobChannelValue = associated.OobChannelValue;
                OobCode = associated.OobCode;
                BarCodeUri = associated.BarCodeUri;
                Secret = associated.Secret;
                VerifiedState = Optional<string>.None;
                return Result.Ok;
            }

            case MfaAuthenticatorChallenged challenged:
            {
                State = MfaAuthenticatorState.Challenged;
                OobChannelValue = challenged.OobChannelValue;
                OobCode = challenged.OobCode;
                BarCodeUri = challenged.BarCodeUri;
                Secret = challenged.Secret;
                VerifiedState = Optional<string>.None;
                return Result.Ok;
            }

            case MfaAuthenticatorConfirmed confirmed:
            {
                IsActive = confirmed.IsActive;
                State = MfaAuthenticatorState.Confirmed;
                VerifiedState = confirmed.VerifiedState;
                return Result.Ok;
            }

            case MfaAuthenticatorVerified verified:
            {
                State = MfaAuthenticatorState.Verified;
                VerifiedState = verified.VerifiedState;
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    public Result<Error> Associate(Optional<PhoneNumber> oobPhoneNumber, Optional<EmailAddress> oobEmailAddress,
        Optional<EmailAddress> otpUsername,
        Optional<string> recoveryCodes)
    {
        if (!IsConfigurable)
        {
            return Error.PreconditionViolation(Resources.MfaAuthenticator_ConfirmAssociation_NotAssociated);
        }

        var oobChannel = Optional<string>.None;
        var oobCode = Optional<string>.None;
        var barCodeUri = Optional<string>.None;
        Optional<string> secret;
        switch (Type)
        {
            case MfaAuthenticatorType.RecoveryCodes:
                if (!recoveryCodes.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_NoRecoveryCodes);
                }

                secret = _encryptionService.Encrypt(recoveryCodes.Value);
                break;

            case MfaAuthenticatorType.OobSms:
            {
                if (!oobPhoneNumber.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_OobSms_NoPhoneNumber);
                }

                oobCode = _mfaService.GenerateOobCode();
                oobChannel = oobPhoneNumber.Value.Number;
                var oobSecret = _mfaService.GenerateOobSecret();
                secret = _encryptionService.Encrypt(oobSecret);
                break;
            }

            case MfaAuthenticatorType.OobEmail:
            {
                if (!oobEmailAddress.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_OobEmail_NoEmailAddress);
                }

                oobCode = _mfaService.GenerateOobCode();
                oobChannel = oobEmailAddress.Value.Address;
                var oobSecret = _mfaService.GenerateOobSecret();
                secret = _encryptionService.Encrypt(oobSecret);
                break;
            }

            case MfaAuthenticatorType.TotpAuthenticator:
                if (!otpUsername.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_OtpAuthenticator_NoUsername);
                }

                var otpSecret = _mfaService.GenerateOtpSecret();
                secret = _encryptionService.Encrypt(otpSecret);
                barCodeUri = _mfaService.GenerateOtpBarcodeUri(otpUsername.Value.Address, otpSecret);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(Type), Type, null);
        }

        return RaiseChangeEvent(Events.PersonCredentials.MfaAuthenticatorAssociated(Id, this, oobCode,
            barCodeUri, secret, oobChannel));
    }

    public Result<Error> Challenge()
    {
        if (!HasBeenConfirmed)
        {
            return Error.PreconditionViolation(Resources.MfaAuthenticator_Challenge_NotConfirmed);
        }

        var oobChannel = OobChannelValue;
        Optional<string> oobCode;
        var barCodeUri = Optional<string>.None;
        Optional<string> secret;
        switch (Type)
        {
            case MfaAuthenticatorType.OobSms:
                if (!oobChannel.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_OobSms_NoPhoneNumber);
                }

                oobCode = _mfaService.GenerateOobCode();
                secret = _encryptionService.Encrypt(_mfaService.GenerateOobSecret());
                break;

            case MfaAuthenticatorType.OobEmail:
                if (!oobChannel.HasValue)
                {
                    return Error.RuleViolation(Resources
                        .MfaAuthenticator_Associate_OobEmail_NoEmailAddress);
                }

                oobCode = _mfaService.GenerateOobCode();
                secret = _encryptionService.Encrypt(_mfaService.GenerateOobSecret());
                break;

            case MfaAuthenticatorType.RecoveryCodes:
            case MfaAuthenticatorType.TotpAuthenticator:
                return Result.Ok;

            case MfaAuthenticatorType.None:
                return Error.RuleViolation(Resources.MfaAuthenticator_Challenge_InvalidAuthenticator);

            default:
                throw new ArgumentOutOfRangeException(nameof(Type), Type, null);
        }

        return RaiseChangeEvent(
            Events.PersonCredentials.MfaAuthenticatorChallenged(Id, this, oobCode, barCodeUri, secret, oobChannel));
    }

    public Result<Error> ConfirmAssociation(Optional<string> confirmationOobCode, Optional<string> confirmationCode)
    {
        if (!IsAssociated)
        {
            return Error.PreconditionViolation(Resources.MfaAuthenticator_ConfirmAssociation_NotAssociated);
        }

        var verifiedState = VerifiedState;
        switch (Type)
        {
            case MfaAuthenticatorType.OobSms:
            case MfaAuthenticatorType.OobEmail:
            {
                if (!OobCode.HasValue
                    || confirmationOobCode != OobCode)
                {
                    return Error.NotAuthenticated();
                }

                var secret = _encryptionService.Decrypt(Secret);
                if (confirmationCode != secret)
                {
                    return Error.NotAuthenticated();
                }

                break;
            }

            case MfaAuthenticatorType.TotpAuthenticator:
            {
                if (!confirmationCode.HasValue)
                {
                    return Error.NotAuthenticated();
                }

                var secret = _encryptionService.Decrypt(Secret);
                var verified = _mfaService.VerifyTotp(secret, new List<long>(), confirmationCode);
                if (!verified.IsValid)
                {
                    return Error.NotAuthenticated();
                }

                var firstTimeStep = verified.TimeStepMatched!.Value.ToString();
                verifiedState = _encryptionService.Encrypt(firstTimeStep);
                break;
            }

            case MfaAuthenticatorType.RecoveryCodes:
            {
                confirmationOobCode = Optional<string>.None;
                confirmationCode = Optional<string>.None;
                break;
            }

            case MfaAuthenticatorType.None:
            {
                return Error.PreconditionViolation(Resources.MfaAuthenticator_ConfirmAssociation_InvalidType);
            }
        }

        return RaiseChangeEvent(Events.PersonCredentials.MfaAuthenticatorConfirmed(RootId,
            this, confirmationOobCode, confirmationCode, verifiedState));
    }

#if TESTINGONLY
    public void TestingOnly_Confirm()
    {
        State = MfaAuthenticatorState.Confirmed;
    }
#endif

    public Result<Error> Verify(Optional<string> confirmationOobCode, Optional<string> confirmationCode)
    {
        if (!HasBeenConfirmed)
        {
            return Error.PreconditionViolation(Resources.MfaAuthenticator_Verify_NotVerifiable);
        }

        var verifiedState = VerifiedState;
        switch (Type)
        {
            case MfaAuthenticatorType.OobSms:
            case MfaAuthenticatorType.OobEmail:
            {
                if (confirmationOobCode != OobCode)
                {
                    return Error.NotAuthenticated();
                }

                var secret = _encryptionService.Decrypt(Secret);
                if (confirmationCode != secret)
                {
                    return Error.NotAuthenticated();
                }

                break;
            }

            case MfaAuthenticatorType.TotpAuthenticator:
            {
                if (!confirmationCode.HasValue)
                {
                    return Error.NotAuthenticated();
                }

                var previousVerifiedState = VerifiedState.HasValue
                    ? _encryptionService.Decrypt(VerifiedState).ToOptional()
                    : Optional<string>.None;
                var previousTimeSteps = ParsePreviousOtpTimeSteps(previousVerifiedState);
                var secret = _encryptionService.Decrypt(Secret);
                var verified = _mfaService.VerifyTotp(secret, previousTimeSteps, confirmationCode);
                if (!verified.IsValid)
                {
                    return Error.NotAuthenticated();
                }

                var latestTimeStep = verified.TimeStepMatched!.Value.ToString();
                var newVerifiedState = AccumulatePreviouslyUsedOtpTimeSteps(previousVerifiedState,
                    latestTimeStep, _mfaService);
                verifiedState = _encryptionService.Encrypt(newVerifiedState);
                break;
            }

            case MfaAuthenticatorType.RecoveryCodes:
            {
                if (!confirmationCode.HasValue)
                {
                    return Error.NotAuthenticated();
                }

                var codes = MfaAuthenticators.ParseRecoveryCodes(_encryptionService, Secret);
                var previousVerifiedState = VerifiedState.HasValue
                    ? _encryptionService.Decrypt(VerifiedState).ToOptional()
                    : Optional<string>.None;
                ExcludePreviouslyUsedRecoveryCodes(codes, previousVerifiedState);
                var matchedCode = Optional<string>.None;
                foreach (var code in codes)
                {
                    if (confirmationCode == code)
                    {
                        matchedCode = code;
                    }
                }

                if (!matchedCode.HasValue)
                {
                    return Error.NotAuthenticated();
                }

                var newVerifiedState = AccumulatePreviouslyUsedRecoveryCodes(previousVerifiedState, matchedCode.Value);
                verifiedState = _encryptionService.Encrypt(newVerifiedState);
                break;
            }

            case MfaAuthenticatorType.None:
            {
                return Error.PreconditionViolation(Resources.MfaAuthenticator_Verify_InvalidType);
            }
        }

        return RaiseChangeEvent(Events.PersonCredentials.MfaAuthenticatorVerified(RootId,
            this, confirmationOobCode, confirmationCode, verifiedState));
    }

    private static void ExcludePreviouslyUsedRecoveryCodes(List<string> recoveryCodes,
        Optional<string> previouslyUsedCodes)
    {
        var usedCodes = MfaAuthenticators.ParseRecoveryCodes(previouslyUsedCodes);
        foreach (var usedCode in usedCodes)
        {
            recoveryCodes.Remove(usedCode);
        }
    }

    /// <summary>
    ///     Accumulates all past used recovery codes
    /// </summary>
    private static string AccumulatePreviouslyUsedRecoveryCodes(string previouslyUsedCodes,
        string usedCode)
    {
        var usedCodes = MfaAuthenticators.ParseRecoveryCodes(previouslyUsedCodes);
        usedCodes.Add(usedCode);

        return usedCodes
            .Where(item => item.HasValue())
            .Distinct()
            .Join(RecoveryCodeDelimiter);
    }

    /// <summary>
    ///     Accumulates up to <see cref="IMfaService.GetTotpMaxTimeSteps" />  past time steps that have already been
    ///     used up. This ensures a rolling window of time steps that cannot be used again.
    ///     There is no need to keep all past time steps since many of these could never be valid again in distant future.
    /// </summary>
    private static string AccumulatePreviouslyUsedOtpTimeSteps(Optional<string> previouslyUsedTimeSteps,
        string? latestTimeStep, IMfaService mfaService)
    {
        var maxTimeStepsToRemember = mfaService.GetTotpMaxTimeSteps();
        var steps = ParsePreviousOtpTimeSteps(previouslyUsedTimeSteps);
        if (latestTimeStep.HasValue())
        {
            var step = latestTimeStep.ToLongOrDefault(-1);
            if (step > 0 && !steps.Contains(step))
            {
                steps.Add(step);
            }
        }

        if (steps.Count > maxTimeStepsToRemember)
        {
            steps = steps
                .TakeLast(maxTimeStepsToRemember)
                .ToList();
        }

        return steps
            .Join(TotpTimeStepDelimiter);
    }

    private static List<long> ParsePreviousOtpTimeSteps(Optional<string> previouslyUsedTimeSteps)
    {
        if (!previouslyUsedTimeSteps.HasValue)
        {
            return [];
        }

        var usedTimeSteps = previouslyUsedTimeSteps.Value
            .Split(TotpTimeStepDelimiter, StringSplitOptions.RemoveEmptyEntries);
        if (usedTimeSteps.Length > 0)
        {
            return usedTimeSteps
                .Select(item => item.ToLongOrDefault(-1))
                .Where(item => item >= 0)
                .Distinct()
                .ToList();
        }

        return [];
    }
}