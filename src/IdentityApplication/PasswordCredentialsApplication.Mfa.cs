using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
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
        var associated = await credential.AssociateMfaAuthenticatorAsync(callerId,
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobPhoneNumber, oobEmailAddress.Value,
            OnAssociate);
        if (associated.IsFailure)
        {
            return associated.Error;
        }

        var authenticator = associated.Value;
        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

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

        return credential.ToAssociatedAuthenticator(authenticator);

        async Task<Result<Error>> OnAssociate(MfaAuthenticator associatedAuthenticator)
        {
            await Task.CompletedTask;

            switch (associatedAuthenticator.Type.Value)
            {
                case MfaAuthenticatorType.OobSms:
                    return await _userNotificationsService.NotifyPasswordMfaOobSmsAsync(caller,
                        associatedAuthenticator.OobChannelValue, associatedAuthenticator.OobCode,
                        UserNotificationConstants.EmailTags.PasswordMfaOob, cancellationToken);

                case MfaAuthenticatorType.OobEmail:
                    return await _userNotificationsService.NotifyPasswordMfaOobEmailAsync(caller,
                        associatedAuthenticator.OobChannelValue, associatedAuthenticator.OobCode,
                        UserNotificationConstants.EmailTags.PasswordMfaOob, cancellationToken);

                default:
                    return Result.Ok;
            }
        }

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

        var retrievedUser = await _endUsersService.GetUserPrivateAsync(caller, caller.ToCallerId(), cancellationToken);
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

    public async Task<Result<AuthenticateTokens, Error>> CompleteMfaAuthenticatorAssociationAsync(ICallerContext caller,
        string? mfaToken, PasswordCredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string completionCode, CancellationToken cancellationToken)
    {
        var authenticated = await AuthenticateUserForMfaInternalAsync(caller, mfaToken, cancellationToken);
        if (authenticated.IsFailure)
        {
            return authenticated.Error;
        }

        var credential = authenticated.Value;
        var completed = credential.CompleteMfaAuthenticatorAssociation(caller.ToCallerId(),
            authenticatorType.ToEnumOrDefault(MfaAuthenticatorType.None), oobCode, completionCode);
        if (completed.IsFailure)
        {
            return completed.Error;
        }

        var saved = await _repository.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credential = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Password credentials for {UserId} has completed association for MFA authenticator {AuthenticatorType}",
            credential.UserId, authenticatorType);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.UserPasswordMfaAssociationCompleted,
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

    /// <summary>
    ///     Authenticates the caller for MFA using either the provided MFA token, or caller
    /// </summary>
    /// <remarks>
    ///     If the caller is not authenticated, the uMFA token must be provided
    /// </remarks>
    private async Task<Result<PasswordCredentialRoot, Error>> AuthenticateUserForMfaInternalAsync(ICallerContext caller,
        string? mfaToken,
        CancellationToken cancellationToken)
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
        this PasswordCredentialRoot credential, MfaAuthenticator authenticator)
    {
        return new AssociatedPasswordCredentialMfaAuthenticator
        {
            Type = authenticator.Type.Value.ToEnum<MfaAuthenticatorType, PasswordCredentialMfaAuthenticatorType>(),
            RecoveryCodes = credential.MfaAuthenticators.GetRecoveryCodes(),
            BarCodeUri = authenticator.BarCodeUri,
            OobCode = authenticator.OobCode
        };
    }

    public static List<PasswordCredentialMfaAuthenticator> ToMfaAuthenticators(this PasswordCredentialRoot credential)
    {
        return credential.MfaAuthenticators
            .Select(auth => new PasswordCredentialMfaAuthenticator
            {
                Id = auth.Id,
                Type = auth.Type.Value.ToEnumOrDefault(PasswordCredentialMfaAuthenticatorType.None),
                IsActive = auth.IsActive
            })
            .ToList();
    }
}