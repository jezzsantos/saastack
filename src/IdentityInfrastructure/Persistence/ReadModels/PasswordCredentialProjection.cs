using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
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
            case Events.PasswordCredentials.Created e:
                return await _credentials.HandleCreateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.RegistrationVerified = false;
                        dto.AccountLocked = false;
                    },
                    cancellationToken);

            case Events.PasswordCredentials.CredentialsChanged e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.PasswordResetToken = Optional<string>.None; }, cancellationToken);

            case Events.PasswordCredentials.RegistrationChanged e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.UserName = e.Name;
                    dto.UserEmailAddress = e.EmailAddress;
                }, cancellationToken);

            case Events.PasswordCredentials.PasswordVerified _:
                return true;

            case Events.PasswordCredentials.AccountLocked e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.AccountLocked = true; },
                    cancellationToken);

            case Events.PasswordCredentials.AccountUnlocked e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.AccountLocked = false; },
                    cancellationToken);

            case Events.PasswordCredentials.RegistrationVerificationCreated e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.RegistrationVerificationToken = e.Token; }, cancellationToken);

            case Events.PasswordCredentials.RegistrationVerificationVerified e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.RegistrationVerificationToken = Optional<string>.None;
                    dto.RegistrationVerified = true;
                }, cancellationToken);

            case Events.PasswordCredentials.PasswordResetInitiated e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.PasswordResetToken = e.Token; }, cancellationToken);

            case Events.PasswordCredentials.PasswordResetCompleted e:
                return await _credentials.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.PasswordResetToken = Optional<string>.None; }, cancellationToken);

            default:
                return false;
        }
    }
}