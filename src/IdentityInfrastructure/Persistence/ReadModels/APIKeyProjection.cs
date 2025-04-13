using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Created = Domain.Events.Shared.Identities.APIKeys.Created;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class APIKeyProjection : IReadModelProjection
{
    private readonly IReadModelStore<APIKeyAuth> _apiKeys;

    public APIKeyProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _apiKeys = new ReadModelStore<APIKeyAuth>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _apiKeys.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.KeyToken = e.KeyToken;
                    },
                    cancellationToken);

            case ParametersChanged e:
                return await _apiKeys.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.Description = e.Description;
                    dto.ExpiresOn = e.ExpiresOn.HasValue
                        ? e.ExpiresOn.Value
                        : Optional<DateTime?>.None;
                }, cancellationToken);

            case KeyVerified _:
                return true;

            case Expired e:
                return await _apiKeys.HandleUpdateAsync(e.RootId, dto => { dto.ExpiresOn = e.ExpiredOn; },
                    cancellationToken);

            case Revoked e:
                return await _apiKeys.HandleUpdateAsync(e.RootId, dto => { dto.RevokedOn = e.RevokedOn; },
                    cancellationToken);

            case Deleted e:
                return await _apiKeys.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(APIKeyRoot);
}