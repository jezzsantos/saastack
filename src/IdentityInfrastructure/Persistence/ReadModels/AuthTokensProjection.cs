using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class AuthTokensProjection : IReadModelProjection
{
    private readonly IReadModelStore<AuthToken> _authTokens;

    public AuthTokensProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _authTokens = new ReadModelStore<AuthToken>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(AuthTokensRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _authTokens.HandleCreateAsync(e.RootId, dto => { dto.UserId = e.UserId; },
                    cancellationToken);

            case TokensChanged e:
                return await _authTokens.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.AccessToken = e.AccessToken;
                    dto.RefreshToken = e.RefreshToken;
                    dto.AccessTokenExpiresOn = e.AccessTokenExpiresOn;
                    dto.RefreshTokenExpiresOn = e.RefreshTokenExpiresOn;
                }, cancellationToken);

            case TokensRefreshed e:
                return await _authTokens.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.AccessToken = e.AccessToken;
                    dto.RefreshToken = e.RefreshToken;
                    dto.AccessTokenExpiresOn = e.AccessTokenExpiresOn;
                    dto.RefreshTokenExpiresOn = e.RefreshTokenExpiresOn;
                }, cancellationToken);

            case TokensRevoked e:
                return await _authTokens.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.AccessToken = Optional<string>.None;
                    dto.RefreshToken = Optional<string>.None;
                    dto.AccessTokenExpiresOn = Optional<DateTime>.None;
                    dto.RefreshTokenExpiresOn = Optional<DateTime>.None;
                }, cancellationToken);

            default:
                return false;
        }
    }
}