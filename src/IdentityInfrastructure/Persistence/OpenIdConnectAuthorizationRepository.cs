using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using IdentityApplication.Persistence;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace IdentityInfrastructure.Persistence;

public class OpenIdConnectAuthorizationRepository : IOpenIdConnectAuthorizationRepository
{
    private readonly ISnapshottingQueryStore<OpenIdConnectAuthorization> _consentQueries;
    private readonly IEventSourcingDddCommandStore<OpenIdConnectAuthorizationRoot> _consents;

    public OpenIdConnectAuthorizationRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OpenIdConnectAuthorizationRoot> consentsStore, IDataStore store)
    {
        _consentQueries = new SnapshottingQueryStore<OpenIdConnectAuthorization>(recorder, domainFactory, store);
        _consents = consentsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _consentQueries.DestroyAllAsync(cancellationToken),
            _consents.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByAccessTokenDigestAsync(
        Identifier userId, string accessTokenDigest, CancellationToken cancellationToken)
    {
        var query = Query.From<OpenIdConnectAuthorization>()
            .Where<string>(auth => auth.UserId, ConditionOperator.EqualTo, userId)
            .AndWhere<string>(auth => auth.AccessTokenDigest, ConditionOperator.EqualTo, accessTokenDigest);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByAuthorizationCodeAsync(
        Identifier clientId, string authorizationCode, CancellationToken cancellationToken)
    {
        var query = Query.From<OpenIdConnectAuthorization>()
            .Where<string>(auth => auth.ClientId, ConditionOperator.EqualTo, clientId)
            .AndWhere<string>(auth => auth.AuthorizationCode, ConditionOperator.EqualTo, authorizationCode);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByClientAndUserAsync(
        Identifier clientId, Identifier userId, CancellationToken cancellationToken)
    {
        var query = Query.From<OpenIdConnectAuthorization>()
            .Where<string>(auth => auth.ClientId, ConditionOperator.EqualTo, clientId)
            .AndWhere<string>(auth => auth.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindByRefreshTokenDigestAsync(
        Identifier clientId, string refreshTokenDigest, CancellationToken cancellationToken)
    {
        var query = Query.From<OpenIdConnectAuthorization>()
            .Where<string>(auth => auth.ClientId, ConditionOperator.EqualTo, clientId)
            .AndWhere<string>(auth => auth.RefreshTokenDigest, ConditionOperator.EqualTo, refreshTokenDigest);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<OpenIdConnectAuthorizationRoot, Error>> SaveAsync(
        OpenIdConnectAuthorizationRoot authorization,
        CancellationToken cancellationToken)
    {
        var saved = await _consents.SaveAsync(authorization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return authorization;
    }

    public async Task<Result<OpenIdConnectAuthorizationRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var authorization = await _consents.LoadAsync(id, cancellationToken);
        if (authorization.IsFailure)
        {
            return authorization.Error;
        }

        return authorization;
    }

    private async Task<Result<Optional<OpenIdConnectAuthorizationRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<OpenIdConnectAuthorization> query, CancellationToken cancellationToken)
    {
        var queried = await _consentQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<OpenIdConnectAuthorizationRoot>.None;
        }

        var authorizations = await _consents.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (authorizations.IsFailure)
        {
            return authorizations.Error;
        }

        return authorizations.Value.ToOptional();
    }
}