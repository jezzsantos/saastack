using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.OAuth2.ClientConsents;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class OAuth2ClientConsentProjection : IReadModelProjection
{
    private readonly IReadModelStore<OAuth2ClientConsent> _consents;

    public OAuth2ClientConsentProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _consents = new ReadModelStore<OAuth2ClientConsent>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _consents.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.ClientId = e.ClientId;
                        dto.UserId = e.UserId;
                        dto.IsConsented = e.IsConsented;
                        dto.Scopes = OAuth2Scopes.Create(e.Scopes).Value;
                    },
                    cancellationToken);

            case ConsentChanged e:
                return await _consents.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.IsConsented = e.IsConsented;
                    dto.Scopes = OAuth2Scopes.Create(e.Scopes).Value;
                }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(OAuth2ClientConsentRoot);
}