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

public class OidcAuthorizationRepository : IOidcAuthorizationRepository
{
    private readonly ISnapshottingQueryStore<OidcAuthorization> _consentQueries;
    private readonly IEventSourcingDddCommandStore<OidcAuthorizationRoot> _consents;

    public OidcAuthorizationRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OidcAuthorizationRoot> consentsStore, IDataStore store)
    {
        _consentQueries = new SnapshottingQueryStore<OidcAuthorization>(recorder, domainFactory, store);
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

    public async Task<Result<Optional<OidcAuthorizationRoot>, Error>> FindByAuthorizationCodeAsync(Identifier clientId,
        string authorizationCode, CancellationToken cancellationToken)
    {
        var query = Query.From<OidcAuthorization>()
            .Where<string>(auth => auth.ClientId, ConditionOperator.EqualTo, clientId)
            .AndWhere<string>(auth => auth.AuthorizationCode, ConditionOperator.EqualTo, authorizationCode);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<OidcAuthorizationRoot>, Error>> FindByClientAndUserAsync(Identifier clientId,
        Identifier userId, CancellationToken cancellationToken)
    {
        var query = Query.From<OidcAuthorization>()
            .Where<string>(auth => auth.ClientId, ConditionOperator.EqualTo, clientId)
            .AndWhere<string>(auth => auth.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<OidcAuthorizationRoot, Error>> SaveAsync(OidcAuthorizationRoot authorization,
        CancellationToken cancellationToken)
    {
        var saved = await _consents.SaveAsync(authorization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return authorization;
    }

    public async Task<Result<OidcAuthorizationRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var authorization = await _consents.LoadAsync(id, cancellationToken);
        if (authorization.IsFailure)
        {
            return authorization.Error;
        }

        return authorization;
    }

    private async Task<Result<Optional<OidcAuthorizationRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<OidcAuthorization> query, CancellationToken cancellationToken)
    {
        var queried = await _consentQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<OidcAuthorizationRoot>.None;
        }

        var authorizations = await _consents.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (authorizations.IsFailure)
        {
            return authorizations.Error;
        }

        return authorizations.Value.ToOptional();
    }
}