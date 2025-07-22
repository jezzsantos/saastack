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

namespace IdentityApplication.ApplicationServices;

public partial class NativeIdentityServerCredentialsService
{
    public const string MfaRequiredCode = "mfa_required";
    public const string MfaTokenName = "MfaToken";

    public async Task<Result<CredentialMfaAuthenticatorAssociation, Error>> AssociateMfaAuthenticatorForUserAsync(
        ICallerContext caller, string userId, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType,
        string? phoneNumber, CancellationToken cancellationToken)
    {
        var retrieved =
            await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), mfaToken, MfaPermittedAccessibility.Both,
                cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var callerId = mfaCaller.CallerId;
        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, callerId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var user = retrievedUser.Value;
        if (user.Classification != EndUserClassification.Person)
        {
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_NotPerson);
        }

        var maintenance = Caller.CreateAsMaintenance(caller);
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
        var associated = await credential.AssociateMfaAuthenticatorAsync(mfaCaller,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobPhoneNumber, oobEmailAddress.Value,
            otpUsername.Value,
            challengedAuthenticator => NotifyUser(caller, challengedAuthenticator, cancellationToken));
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

    public async Task<Result<CredentialMfaAuthenticatorChallenge, Error>> ChallengeMfaAuthenticatorForUserAsync(
        ICallerContext caller, string userId, string mfaToken, string authenticatorId,
        CancellationToken cancellationToken)
    {
        var retrieved = await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), mfaToken,
            MfaPermittedAccessibility.UnauthenticatedOnly, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var challenged = await credential.ChallengeMfaAuthenticatorAsync(mfaCaller, authenticatorId.ToId(),
            challengedAuthenticator => NotifyUser(caller, challengedAuthenticator, cancellationToken));
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

    public async Task<Result<PersonCredential, Error>> ChangeMfaForUserAsync(ICallerContext caller, string userId,
        bool isEnabled, CancellationToken cancellationToken)
    {
        var retrievedCredential =
            await _repository.FindCredentialByUserIdAsync(userId.ToId(), cancellationToken);
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
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_NotPerson);
        }

        var credential = retrievedCredential.Value.Value;
        var changed = credential.ChangeMfaEnabled(userId.ToId(), isEnabled);
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

    public async Task<Result<CredentialMfaAuthenticatorConfirmation, Error>>
        ConfirmMfaAuthenticatorAssociationForUserAsync(ICallerContext caller, string userId, string? mfaToken,
            CredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
            CancellationToken cancellationToken)
    {
        var retrieved =
            await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), mfaToken, MfaPermittedAccessibility.Both,
                cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var confirmed = credential.ConfirmMfaAuthenticatorAssociation(mfaCaller,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobCode, confirmationCode);
        if (confirmed.IsFailure)
        {
            if (confirmed.Error.Code == ErrorCode.NotAuthenticated)
            {
                _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
                    Audits.PersonCredentialsApplication_MfaAuthenticate_Failed,
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
            Audits.PersonCredentialsApplication_MfaAuthenticate_Succeeded,
            "User {Id} succeeded to authenticate with MFA factor {AuthenticatorType}", credential.UserId,
            authenticatorType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaAuthenticated,
            new Dictionary<string, object>
            {
                { nameof(credential.Id), credential.UserId },
                { UsageConstants.Properties.MfaAuthenticatorType, authenticatorType }
            });

        if (caller.IsAuthenticated)
        {
            return new CredentialMfaAuthenticatorConfirmation
            {
                Tokens = null,
                Authenticators = credential.ToMfaAuthenticators()
            };
        }

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, credential.UserId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
        var tokens = await IssueAuthenticationTokensAsync(caller, user, cancellationToken);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        return new CredentialMfaAuthenticatorConfirmation
        {
            Tokens = tokens.Value,
            Authenticators = credential.ToMfaAuthenticators()
        };
    }

    public async Task<Result<Error>> DisassociateMfaAuthenticatorForUserAsync(ICallerContext caller, string userId,
        string authenticatorId,
        CancellationToken cancellationToken)
    {
        var retrieved = await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), null,
            MfaPermittedAccessibility.AuthenticatedOnly, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var disassociated = credential.DisassociateMfaAuthenticator(mfaCaller, authenticatorId.ToId());
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

    public async Task<Result<List<CredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsForUserAsync(
        ICallerContext caller, string userId, string? mfaToken, CancellationToken cancellationToken)
    {
        var retrieved =
            await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), mfaToken, MfaPermittedAccessibility.Both,
                cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var viewed = credential.ViewMfaAuthenticators(mfaCaller);
        if (viewed.IsFailure)
        {
            return viewed.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} have had the MFA authenticators retrieved",
            credential.UserId);

        return credential.ToMfaAuthenticators();
    }

    public async Task<Result<PersonCredential, Error>> ResetPasswordMfaForUserAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken)
    {
        var retrievedCredential =
            await _repository.FindCredentialByUserIdAsync(userId.ToId(), cancellationToken);
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
            return Error.PreconditionViolation(Resources.PersonCredentialsApplication_NotPerson);
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
            credential.UserId, userId);
        _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
            Audits.PersonCredentialsApplication_MfaReset,
            "User {Id} had their MFA state reset by {Operator}", credential.UserId, userId);

        return credential.ToCredential(user);
    }

    public async Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorForUserAsync(ICallerContext caller,
        string userId, string mfaToken, CredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string confirmationCode, CancellationToken cancellationToken)
    {
        var retrieved = await FindAuthenticatedMfaUserAsync(caller, userId.ToId(), mfaToken,
            MfaPermittedAccessibility.UnauthenticatedOnly, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var credential = retrieved.Value.Credential;
        var mfaCaller = retrieved.Value.Caller;
        var verified = credential.VerifyMfaAuthenticator(mfaCaller,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobCode, confirmationCode);
        if (verified.IsFailure)
        {
            if (verified.Error.Code == ErrorCode.NotAuthenticated)
            {
                _recorder.AuditAgainst(caller.ToCall(), credential.UserId,
                    Audits.PersonCredentialsApplication_MfaAuthenticate_Failed,
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
            Audits.PersonCredentialsApplication_MfaAuthenticate_Succeeded,
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

    private async Task<Result<Error>> NotifyUser(ICallerContext caller, MfaAuthenticator authenticator,
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
    private async Task<Result<(PersonCredentialRoot Credential, MfaCaller Caller), Error>>
        FindAuthenticatedMfaUserAsync(ICallerContext caller, Identifier userId, string? mfaToken,
            MfaPermittedAccessibility accessibility,
            CancellationToken cancellationToken)
    {
        switch (accessibility)
        {
            case MfaPermittedAccessibility.UnauthenticatedOnly:
            {
                if (caller.IsAuthenticated)
                {
                    return Error.ForbiddenAccess();
                }

                return await FindUserByMfaTokenAsync();
            }

            case MfaPermittedAccessibility.AuthenticatedOnly:
            {
                if (!caller.IsAuthenticated)
                {
                    return Error.ForbiddenAccess();
                }

                return await FindUserByUserIdAsync();
            }

            case MfaPermittedAccessibility.Both:
            {
                if (caller.IsAuthenticated)
                {
                    return await FindUserByUserIdAsync();
                }

                return await FindUserByMfaTokenAsync();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
        }

        async Task<Result<(PersonCredentialRoot Credential, MfaCaller Caller), Error>> FindUserByMfaTokenAsync()
        {
            if (mfaToken.HasNoValue())
            {
                return Error.NotAuthenticated();
            }

            var retrieved =
                await _repository.FindCredentialByMfaAuthenticationTokenAsync(mfaToken, cancellationToken);
            if (retrieved.IsFailure)
            {
                return retrieved.Error;
            }

            if (!retrieved.Value.HasValue)
            {
                return Error.NotAuthenticated();
            }

            var credential = retrieved.Value.Value;
            var mfaCaller = MfaCaller.Create(credential.UserId, mfaToken);
            if (mfaCaller.IsFailure)
            {
                return mfaCaller.Error;
            }

            return (credential, mfaCaller.Value);
        }

        async Task<Result<(PersonCredentialRoot Credential, MfaCaller Caller), Error>> FindUserByUserIdAsync()
        {
            var retrieved =
                await _repository.FindCredentialByUserIdAsync(userId, cancellationToken);
            if (retrieved.IsFailure)
            {
                return retrieved.Error;
            }

            if (!retrieved.Value.HasValue)
            {
                return Error.EntityNotFound();
            }

            var credential = retrieved.Value.Value;
            var mfaCaller = MfaCaller.Create(credential.UserId, null);
            if (mfaCaller.IsFailure)
            {
                return mfaCaller.Error;
            }

            return (credential, mfaCaller.Value);
        }
    }

    private enum MfaPermittedAccessibility
    {
        UnauthenticatedOnly = 0,
        AuthenticatedOnly = 1,
        Both = 2
    }
}

internal static class NativeIdentityServerCredentialsServiceMfaConversionExtensions
{
    public static CredentialMfaAuthenticatorAssociation ToAssociatedAuthenticator(
        this PersonCredentialRoot personCredential, MfaAuthenticator authenticator, bool showRecoveryCodes,
        IEncryptionService encryptionService)
    {
        var secret = authenticator.Type == MfaAuthenticatorType.TotpAuthenticator
            ? encryptionService.Decrypt(authenticator.Secret)
            : null;
        return new CredentialMfaAuthenticatorAssociation
        {
            Type = authenticator.Type.ToEnum<MfaAuthenticatorType, CredentialMfaAuthenticatorType>(),
            RecoveryCodes = showRecoveryCodes
                ? personCredential.MfaAuthenticators.ToRecoveryCodes(encryptionService)
                : null,
            BarCodeUri = authenticator.BarCodeUri,
            OobCode = authenticator.OobCode,
            Secret = secret
        };
    }

    public static CredentialMfaAuthenticatorChallenge ToChallengedAuthenticator(
        this MfaAuthenticator authenticator)
    {
        return new CredentialMfaAuthenticatorChallenge
        {
            OobCode = authenticator.OobCode,
            Type = authenticator.Type.ToEnum<MfaAuthenticatorType, CredentialMfaAuthenticatorType>()
        };
    }

    public static List<CredentialMfaAuthenticator> ToMfaAuthenticators(this PersonCredentialRoot personCredential)
    {
        return personCredential.MfaAuthenticators
            .Select(auth => new CredentialMfaAuthenticator
            {
                Id = auth.Id,
                Type = auth.Type.ToEnumOrDefault(CredentialMfaAuthenticatorType.None),
                IsActive = auth.IsActive
            })
            .ToList();
    }
}