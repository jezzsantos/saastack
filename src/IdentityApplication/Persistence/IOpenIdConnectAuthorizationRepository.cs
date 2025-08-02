using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IOpenIdConnectAuthorizationRepository : IApplicationRepository
{
    Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByAccessTokenDigestAsync(Identifier userId,
        string accessTokenDigest,
        CancellationToken cancellationToken);

    Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByAuthorizationCodeAsync(Identifier clientId,
        string authorizationCode,
        CancellationToken cancellationToken);

    Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByClientAndUserAsync(Identifier clientId,
        Identifier userId, CancellationToken cancellationToken);

    Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByRefreshTokenDigestAsync(Identifier clientId,
        string refreshTokenDigest, CancellationToken cancellationToken);

    Task<Result<OpenIdConnectAuthorizationRoot, Error>> SaveAsync(OpenIdConnectAuthorizationRoot authorization,
        CancellationToken cancellationToken);
}