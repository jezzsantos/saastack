using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using IdentityDomain;

namespace IdentityApplication;

partial class PasswordCredentialsApplication
{
    public const string MfaRequiredCode = "mfa_required";
    public const string MfaTokenName = "MfaToken";

    public async Task<Result<AssociatedPasswordCredentialMfaAuthenticator, Error>> AssociateMfaAuthenticatorAsync(
        ICallerContext caller, string? mfaToken, PasswordCredentialMfaAuthenticatorType authenticatorType,
        string? phoneNumber, CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        var callerId = authenticated.Value.UserId;
        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, callerId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_NotPerson);
        }

        var maintenance = Caller.CreateAsMaintenance(caller.CallId);
        var retrievedProfile =
            await _userProfilesService.GetProfilePrivateAsync(maintenance, callerId, cancellationToken);
        if (retrievedProfile.IsFailure)
        {
            return retrievedProfile.Error;
        }

        var userProfile = retrievedProfile.Value;
        var oobPhoneNumber = DerivePhoneNumber(phoneNumber, userProfile);
        var oobEmailAddress = DeriveEmailAddress(userProfile);
        var otpUsername = oobEmailAddress;
        var associated = await credential.AssociateMfaAuthenticatorAsync(caller.IsAuthenticated, callerId,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobPhoneNumber, oobEmailAddress.Value,
            otpUsername.Value,
            challengedAuthenticator => ChallengeAuthenticator(caller, challengedAuthenticator, cancellationToken));
        if (associated.IsFailure)
        {
            return associated.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        var authenticator = associated.Value;
        var isFirstAuthenticator = credential.MfaAuthenticators.HasOnlyOneUnconfirmedPlusRecoveryCodes;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} is associating MFA authenticator {AuthenticatorType}",
            credential.UserId, authenticatorType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaAssociationStarted,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, authenticatorType }
            });

        return credential.ToAssociatedAuthenticator(authenticator, isFirstAuthenticator, _encryptionService);

        static Optional<PhoneNumber> DerivePhoneNumber(string? phoneNumber, UserProfile userProfile)
        {
            if (phoneNumber.HasValue())
            {
                var phone = PhoneNumber.Create(phoneNumber);
                if (phone.IsFailure)
                {
                    return Optional<PhoneNumber>.None;
                }

                return phone.Value;
            }

            if (userProfile.PhoneNumber.HasValue())
            {
                var phone = PhoneNumber.Create(userProfile.PhoneNumber);
                if (phone.IsFailure)
                {
                    return Optional<PhoneNumber>.None;
                }

                return phone.Value;
            }

            return Optional<PhoneNumber>.None;
        }

        static Optional<EmailAddress> DeriveEmailAddress(UserProfile userProfile)
        {
            if (userProfile.EmailAddress.HasValue())
            {
                var email = EmailAddress.Create(userProfile.EmailAddress);
                if (email.IsFailure)
                {
                    return Optional<EmailAddress>.None;
                }

                return email.Value;
            }

            return Optional<EmailAddress>.None;
        }
    }

    public async Task<Result<PasswordCredentialChallenge, Error>> ChallengeMfaAuthenticatorAsync(ICallerContext caller,
        string? mfaToken, string authenticatorId, CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        var callerId = authenticated.Value.UserId;
        var challenged = await credential.ChallengeMfaAuthenticatorAsync(callerId, authenticatorId.ToId(),
            challengedAuthenticator => ChallengeAuthenticator(caller, challengedAuthenticator, cancellationToken));
        if (challenged.IsFailure)
        {
            return challenged.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        var authenticator = challenged.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} is challenging MFA authenticator {AuthenticatorType}",
            credential.UserId, authenticator.Type);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaChallenge,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, authenticator.Type }
            });

        return authenticator.ToChallengedAuthenticator();
    }

    public async Task<Result<PasswordCredential, Error>> ChangeMfaAsync(ICallerContext caller,
        bool isEnabled, CancellationToken cancellationToken)
    {
        var retrievedCredential =
            await _repository.FindCredentialsByUserIdAsync(caller.ToCallerId(), cancellationToken);
        if (retrievedCredential.IsFailure)
        {
            return retrievedCredential.Error;
        }

        if (!retrievedCredential.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, caller.CallerId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_NotPerson);
        }

        var credential = retrievedCredential.Value.Value;
        var changed = credential.ChangeMfaEnabled(caller.ToCallerId(), isEnabled);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Password credentials for {UserId} has been enabled for MFA",
            credential.UserId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaEnabled,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.Enabled, isEnabled }
            });

        return credential.ToCredential(user);
    }

    public async Task<Result<AuthenticateTokens, Error>> ConfirmMfaAuthenticatorAssociationAsync(ICallerContext caller,
        string? mfaToken, PasswordCredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string confirmationCode, CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        var callerId = authenticated.Value.UserId;
        var confirmed = credential.ConfirmMfaAuthenticatorAssociation(callerId,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobCode, confirmationCode);
        if (confirmed.IsFailure)
        {
            if (confirmed.Error.Code == ErrorCode.NotAuthenticated)
            {
                _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
                    Audits.PasswordCredentialsApplication_MfaAuthenticate_Failed,
                    "User {Id} failed to authenticate with invalid 2FA", credential.UserId);
            }

            return confirmed.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} has successfully authenticated for MFA authenticator {AuthenticatorType}",
            credential.UserId, authenticatorType);
        _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
            Audits.PasswordCredentialsApplication_MfaAuthenticate_Succeeded,
            "User {Id} succeeded to authenticate with MFA factor {AuthenticatorType}", credential.UserId,
            authenticatorType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaAuthenticated,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, authenticatorType }
            });

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, credential.UserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
        return await IssueAuthenticationTokensAsync(caller, user, cancellationToken);
    }

    public async Task<Result<Error>> DisassociateMfaAuthenticatorAsync(ICallerContext caller, string authenticatorId,
        CancellationToken cancellationToken)
    {
        var retrieved =
            await _repository.FindCredentialsByUserIdAsync(caller.ToCallerId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credential = retrieved.Value.Value;
        var disassociated = credential.DisassociateMfaAuthenticator(caller.ToCallerId(), authenticatorId.ToId());
        if (disassociated.IsFailure)
        {
            return disassociated.Error;
        }

        var disassociatedType = disassociated.Value.Type;
        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} has disassociated MFA authenticator {AuthenticatorType}",
            credential.UserId, disassociatedType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaDisassociated,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, disassociatedType }
            });
        return Result.Ok;
    }

    public async Task<Result<List<PasswordCredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsAsync(
        ICallerContext caller, string? mfaToken, CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} have had the MFA authenticators retrieved",
            credential.UserId);

        return credential.ToMfaAuthenticators();
    }

    public async Task<Result<PasswordCredential, Error>> ResetPasswordMfaAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        var retrievedCredential =
            await _repository.FindCredentialsByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrievedCredential.IsFailure)
        {
            return retrievedCredential.Error;
        }

        if (!retrievedCredential.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.PreconditionViolation(Resources.PasswordCredentialsApplication_NotPerson);
        }

        var credential = retrievedCredential.Value.Value;
        var resetterRoles = Roles.Create(caller.Roles.All);
        if (resetterRoles.IsFailure)
        {
            return resetterRoles.Error;
        }

        var reset = credential.ResetMfa(resetterRoles.Value);
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Password credentials for {UserId} has had MFA reset by {Operator}",
            credential.UserId, caller.CallerId);
        _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
            Audits.PasswordCredentialsApplication_MfaReset,
            "User {Id} had their MFA state reset by {Operator}", credential.UserId, caller.CallerId);

        return credential.ToCredential(user);
    }

    public async Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorAsync(ICallerContext caller,
        string mfaToken,
        PasswordCredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
        CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        var callerId = authenticated.Value.UserId;
        var verified = credential.VerifyMfaAuthenticator(caller.IsAuthenticated, callerId,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobCode, confirmationCode);
        if (verified.IsFailure)
        {
            if (verified.Error.Code == ErrorCode.NotAuthenticated)
            {
                _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
                    Audits.PasswordCredentialsApplication_MfaAuthenticate_Failed,
                    "User {Id} failed to authenticate with invalid 2FA", credential.UserId);
            }

            return verified.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} has successfully authenticated for MFA authenticator {AuthenticatorType}",
            credential.UserId, authenticatorType);
        _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
            Audits.PasswordCredentialsApplication_MfaAuthenticate_Succeeded,
            "User {Id} succeeded to authenticate with MFA factor {AuthenticatorType}", credential.UserId,
            authenticatorType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaAuthenticated,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, authenticatorType }
            });

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, credential.UserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
        return await IssueAuthenticationTokensAsync(caller, user, cancellationToken);
    }

    private async Task<Result<Error>> ChallengeAuthenticator(ICallerContext caller, MfaAuthenticator authenticator,
        CancellationToken cancellationToken)
    {
        switch (authenticator.Type)
        {
            case MfaAuthenticatorType.OobSms:
            {
                var secret = _encryptionService.Decrypt(authenticator.Secret);
                return await _userNotificationsService.NotifyPasswordMfaOobSmsAsync(caller,
                    authenticator.OobChannelValue, secret,
                    UserNotificationConstants.EmailTags.PasswordMfaOob, cancellationToken);
            }

            case MfaAuthenticatorType.OobEmail:
            {
                var secret = _encryptionService.Decrypt(authenticator.Secret);
                return await _userNotificationsService.NotifyPasswordMfaOobEmailAsync(caller,
                    authenticator.OobChannelValue, secret,
                    UserNotificationConstants.EmailTags.PasswordMfaOob, cancellationToken);
            }

            default:
                return Result.Ok;
        }
    }

    /// <summary>
    ///     Authenticates the caller for MFA using either the provided MFA token, or caller
    /// </summary>
    /// <remarks>
    ///     If the caller is not authenticated, the uMFA token must be provided
    /// </remarks>
    private async Task<Result<PasswordCredentialRoot, Error>> AuthenticateUserForMfaInternalAsync(ICallerContext caller,
        string? mfaToken, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated
            && mfaToken.HasNoValue())
        {
            return Error.NotAuthenticated();
        }

        PasswordCredentialRoot credential;
        if (caller.IsAuthenticated)
        {
            var retrieved =
                await _repository.FindCredentialsByUserIdAsync(caller.ToCallerId(), cancellationToken);
            if (retrieved.IsFailure)
            {
                return retrieved.Error;
            }

            if (!retrieved.Value.HasValue)
            {
                return Error.NotAuthenticated();
            }

            credential = retrieved.Value.Value;
        }
        else
        {
            var retrieved =
                await _repository.FindCredentialsByMfaAuthenticationTokenAsync(mfaToken!, cancellationToken);
            if (retrieved.IsFailure)
            {
                return retrieved.Error;
            }

            if (!retrieved.Value.HasValue)
            {
                return Error.NotAuthenticated();
            }

            credential = retrieved.Value.Value;
        }

        var authenticated = credential.MfaAuthenticate(caller.IsAuthenticated, mfaToken!);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        return credential;
    }
}

internal static class PasswordCredentialMfaConversionExtensions
{
    public static AssociatedPasswordCredentialMfaAuthenticator ToAssociatedAuthenticator(
        this PasswordCredentialRoot credential, MfaAuthenticator authenticator, bool showRecoveryCodes,
        IEncryptionService encryptionService)
    {
        var secret = authenticator.Type == MfaAuthenticatorType.TotpAuthenticator
            ? encryptionService.Decrypt(authenticator.Secret)
            : null;
        return new AssociatedPasswordCredentialMfaAuthenticator
        {
            Type = authenticator.Type.ToEnum<MfaAuthenticatorType, PasswordCredentialMfaAuthenticatorType>(),
            RecoveryCodes = showRecoveryCodes
                ? credential.MfaAuthenticators.ToRecoveryCodes(encryptionService)
                : null,
            BarCodeUri = authenticator.BarCodeUri,
            OobCode = authenticator.OobCode,
            Secret = secret
        };
    }

    public static PasswordCredentialChallenge ToChallengedAuthenticator(this MfaAuthenticator authenticator)
    {
        return new PasswordCredentialChallenge
        {
            OobCode = authenticator.OobCode,
            Type = authenticator.Type.ToEnum<MfaAuthenticatorType, PasswordCredentialMfaAuthenticatorType>()
        };
    }

    public static List<PasswordCredentialMfaAuthenticator> ToMfaAuthenticators(this PasswordCredentialRoot credential)
    {
        return credential.MfaAuthenticators
            .Select(auth => new PasswordCredentialMfaAuthenticator
            {
                Id = auth.Id,
                Type = auth.Type.ToEnumOrDefault(PasswordCredentialMfaAuthenticatorType.None),
                IsActive = auth.IsActive
            })
            .ToList();
    }
}