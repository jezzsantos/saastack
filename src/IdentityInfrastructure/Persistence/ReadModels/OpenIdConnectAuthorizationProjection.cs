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

public class OpenIdConnectAuthorizationProjection : IReadModelProjection
{
    private readonly IReadModelStore<OpenIdConnectAuthorization> _authorizations;

    public OpenIdConnectAuthorizationProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _authorizations = new ReadModelStore<OpenIdConnectAuthorization>(recorder, domainFactory, store);
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
                        dto.AuthorizedAt = e.AuthorizedAt;
                        dto.AuthorizationCode = e.Code;
                        dto.AuthorizationExpiresAt = e.ExpiresAt;
                        dto.CodeChallenge = e.CodeChallenge;
                        dto.CodeChallengeMethod =
                            e.CodeChallengeMethod ?? Optional<OpenIdConnectCodeChallengeMethod>.None;
                        dto.Nonce = e.Nonce;
                        dto.RedirectUri = e.RedirectUri;
                        dto.Scopes = OAuth2Scopes.Create(e.Scopes).Value;
                    }, cancellationToken);

            case CodeExchanged e:
                return await _authorizations.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.AuthorizationCode = Optional<string>.None;
                        dto.AuthorizationExpiresAt = Optional<DateTime>.None;
                        dto.CodeExchangedAt = e.ExchangedAt;
                        dto.AccessTokenDigest = e.AccessTokenDigest;
                        dto.AccessTokenExpiresAt = e.AccessTokenExpiresOn.HasValue
                            ? e.AccessTokenExpiresOn.Value
                            : Optional<DateTime>.None;
                        dto.RefreshTokenDigest = e.RefreshTokenDigest;
                        dto.RefreshTokenExpiresAt = e.RefreshTokenExpiresOn.HasValue
                            ? e.RefreshTokenExpiresOn.Value
                            : Optional<DateTime>.None;
                    }, cancellationToken);

            case TokenRefreshed e:
                return await _authorizations.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.LastRefreshedAt = e.RefreshedAt;
                        dto.AccessTokenDigest = e.AccessTokenDigest;
                        dto.AccessTokenExpiresAt = e.AccessTokenExpiresOn.HasValue
                            ? e.AccessTokenExpiresOn.Value
                            : Optional<DateTime>.None;
                        dto.RefreshTokenDigest = e.RefreshTokenDigest;
                        dto.RefreshTokenExpiresAt = e.RefreshTokenExpiresOn.HasValue
                            ? e.RefreshTokenExpiresOn.Value
                            : Optional<DateTime>.None;
                        dto.Scopes = OAuth2Scopes.Create(e.Scopes).Value;
                    }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(OpenIdConnectAuthorizationRoot);
}