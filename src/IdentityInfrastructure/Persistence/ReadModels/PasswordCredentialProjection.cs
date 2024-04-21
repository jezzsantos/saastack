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

namespace IdentityInfrastructure.Persistence.ReadModels;

public class PasswordCredentialProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<PasswordCredential> _credentials;

    public PasswordCredentialProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _credentials = new ReadModelProjectionStore<PasswordCredential>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(PasswordCredentialRoot);

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

            default:
                return false;
        }
    }
}