using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Shared.Identities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class OidcAuthorizationProjection : IReadModelProjection
{
    private readonly IReadModelStore<OidcAuthorization> _authorizations;

    public OidcAuthorizationProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _authorizations = new ReadModelStore<OidcAuthorization>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _authorizations.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.ClientId = e.ClientId;
                        dto.UserId = e.UserId;
                    },
                    cancellationToken);

            case CodeAuthorized e:
                return await _authorizations.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.AuthorizationCode = e.Code;
                        dto.AuthorizationExpiresAt = e.ExpiresAt;
                        dto.Scopes = OAuth2Scopes.Create(e.Scopes).Value;
                        dto.RedirectUri = e.RedirectUri;
                        dto.Nonce = e.Nonce;
                        dto.CodeChallenge = e.CodeChallenge;
                        dto.CodeChallengeMethod = e.CodeChallengeMethod ?? Optional<OAuth2CodeChallengeMethod>.None;
                    }, cancellationToken);

            case CodeExchanged e:
                return await _authorizations.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.AuthorizedAt = e.ExchangedAt;
                        dto.AuthorizationCode = Optional<string>.None;
                        dto.AuthorizationExpiresAt = Optional<DateTime>.None;
                    }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(OidcAuthorizationRoot);
}