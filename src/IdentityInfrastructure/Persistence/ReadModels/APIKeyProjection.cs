using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Events;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class APIKeyProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<APIKey> _authTokens;

    public APIKeyProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _authTokens = new ReadModelProjectionStore<APIKey>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(APIKeyRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _authTokens.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.KeyToken = e.KeyToken;
                    },
                    cancellationToken);

            case ParametersChanged e:
                return await _authTokens.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.Description = e.Description;
                    dto.ExpiresOn = e.ExpiresOn.ToOptional();
                }, cancellationToken);

            case KeyVerified _:
                return true;

            case Global.StreamDeleted e:
                return await _authTokens.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }
}