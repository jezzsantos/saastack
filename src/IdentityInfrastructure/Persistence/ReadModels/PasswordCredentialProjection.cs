using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using MfaAuthenticator = IdentityApplication.Persistence.ReadModels.MfaAuthenticator;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class PasswordCredentialProjection : IReadModelProjection
{
    private readonly IReadModelStore<PasswordCredential> _credentials;
    private readonly IReadModelStore<MfaAuthenticator> _mfaAuthenticators;

    public PasswordCredentialProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _credentials = new ReadModelStore<PasswordCredential>(recorder, domainFactory, store);
        _mfaAuthenticators = new ReadModelStore<MfaAuthenticator>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _credentials.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.RegistrationVerified = false;
                        dto.AccountLocked = false;
                        dto.IsMfaEnabled = e.IsMfaEnabled;
                        dto.MfaCanBeDisabled = e.MfaCanBeDisabled;
                    },
                    cancellationToken);

            case CredentialsChanged e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.PasswordResetToken = Optional<string>.None; }, cancellationToken);

            case RegistrationChanged e:
                return await _credentials.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.UserName = e.Name;
                    dto.UserEmailAddress = e.EmailAddress;
                }, cancellationToken);

            case PasswordVerified _:
                return true;

            case AccountLocked e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.AccountLocked = true; },
                    cancellationToken);

            case AccountUnlocked e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.AccountLocked = false; },
                    cancellationToken);

            case RegistrationVerificationCreated e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.RegistrationVerificationToken = e.Token; }, cancellationToken);

            case RegistrationVerificationVerified e:
                return await _credentials.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.RegistrationVerificationToken = Optional<string>.None;
                    dto.RegistrationVerified = true;
                }, cancellationToken);

            case PasswordResetInitiated e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.PasswordResetToken = e.Token; }, cancellationToken);

            case PasswordResetCompleted e:
                return await _credentials.HandleUpdateAsync(e.RootId,
                    dto => { dto.PasswordResetToken = Optional<string>.None; }, cancellationToken);

            case MfaOptionsChanged e:
                return await _credentials.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.IsMfaEnabled = e.IsEnabled;
                        dto.MfaCanBeDisabled = e.CanBeDisabled;
                    },
                    cancellationToken);

            case MfaStateReset e:
                return await _credentials.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.IsMfaEnabled = e.IsEnabled;
                        dto.MfaCanBeDisabled = e.CanBeDisabled;
                    },
                    cancellationToken);

            case MfaAuthenticationInitiated e:
                return await _credentials.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.MfaAuthenticationToken = e.AuthenticationToken;
                        dto.MfaAuthenticationExpiresAt = e.AuthenticationExpiresAt;
                    },
                    cancellationToken);

            case MfaAuthenticatorAdded e:
                return await _mfaAuthenticators.HandleCreateAsync(e.AuthenticatorId!, dto =>
                    {
                        dto.PasswordCredentialId = e.RootId;
                        dto.UserId = e.UserId;
                        dto.Type = e.Type;
                        dto.IsActive = e.IsActive;
                        dto.State = MfaAuthenticatorState.Created;
                        dto.VerifiedState = Optional<string>.None;
                    },
                    cancellationToken);

            case MfaAuthenticatorRemoved e:
                return await _mfaAuthenticators.HandleDeleteAsync(e.AuthenticatorId, cancellationToken);

            case MfaAuthenticatorAssociated e:
                return await _mfaAuthenticators.HandleUpdateAsync(e.AuthenticatorId, dto =>
                    {
                        dto.State = MfaAuthenticatorState.Associated;
                        dto.OobCode = e.OobCode;
                        dto.OobChannelValue = e.OobChannelValue;
                        dto.BarCodeUri = e.BarCodeUri;
                        dto.Secret = e.Secret;
                        dto.VerifiedState = Optional<string>.None;
                    },
                    cancellationToken);

            case MfaAuthenticatorConfirmed e:
                return await _mfaAuthenticators.HandleUpdateAsync(e.AuthenticatorId, dto =>
                    {
                        dto.State = MfaAuthenticatorState.Confirmed;
                        dto.VerifiedState = e.VerifiedState;
                        dto.IsActive = e.IsActive;
                    },
                    cancellationToken);

            case MfaAuthenticatorChallenged e:
                return await _mfaAuthenticators.HandleUpdateAsync(e.AuthenticatorId, dto =>
                    {
                        dto.State = MfaAuthenticatorState.Challenged;
                        dto.OobCode = e.OobCode;
                        dto.OobChannelValue = e.OobChannelValue;
                        dto.BarCodeUri = e.BarCodeUri;
                        dto.Secret = e.Secret;
                        dto.VerifiedState = Optional<string>.None;
                    },
                    cancellationToken);

            case MfaAuthenticatorVerified e:
                return await _mfaAuthenticators.HandleUpdateAsync(e.AuthenticatorId, dto =>
                    {
                        dto.State = MfaAuthenticatorState.Verified;
                        dto.VerifiedState = e.VerifiedState;
                    },
                    cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(PasswordCredentialRoot);
}