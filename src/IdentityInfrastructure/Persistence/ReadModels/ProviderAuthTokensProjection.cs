using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.ProviderAuthTokens;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Created = Domain.Events.Shared.Identities.SSOUsers.Created;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class ProviderAuthTokensProjection : IReadModelProjection
{
    private readonly IReadModelStore<ProviderAuthTokens> _tokens;

    public ProviderAuthTokensProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _tokens = new ReadModelStore<ProviderAuthTokens>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _tokens.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.ProviderName = e.ProviderName;
                    },
                    cancellationToken);

            case TokensChanged e:
                return await _tokens.HandleUpdateAsync(e.RootId,
                    dto => { dto.Tokens = AuthTokens.Create(e.Tokens).Value; }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(ProviderAuthTokensRoot);
}