using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.OAuth2.Clients;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class OAuth2ClientProjection : IReadModelProjection
{
    private readonly IReadModelStore<OAuth2Client> _clients;

    public OAuth2ClientProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _clients = new ReadModelStore<OAuth2Client>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _clients.HandleCreateAsync(e.RootId, dto => { dto.Name = e.Name; },
                    cancellationToken);

            case NameChanged e:
                return await _clients.HandleUpdateAsync(e.RootId, dto => { dto.Name = e.Name; }, cancellationToken);

            case RedirectUriChanged e:
                return await _clients.HandleUpdateAsync(e.RootId, dto => { dto.RedirectUri = e.RedirectUri; },
                    cancellationToken);

            case SecretAdded _:
                return true;
            
            case Deleted e:
                return await _clients.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(OAuth2ClientRoot);
}