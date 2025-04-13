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

public class SSOUsersRepository : ISSOUsersRepository
{
    private readonly ISnapshottingDddCommandStore<ProviderAuthTokensRoot> _providerTokens;
    private readonly ISnapshottingQueryStore<ProviderAuthTokens> _providerTokensQueries;
    private readonly ISnapshottingQueryStore<SSOUser> _userQueries;
    private readonly IEventSourcingDddCommandStore<SSOUserRoot> _users;

    public SSOUsersRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<SSOUserRoot> usersStore, IDataStore store)
    {
        _userQueries = new SnapshottingQueryStore<SSOUser>(recorder, domainFactory, store);
        _users = usersStore;
        _providerTokensQueries = new SnapshottingQueryStore<ProviderAuthTokens>(recorder, domainFactory, store);
        _providerTokens = new SnapshottingDddCommandStore<ProviderAuthTokensRoot>(recorder, domainFactory, store);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _userQueries.DestroyAllAsync(cancellationToken),
            _users.DestroyAllAsync(cancellationToken),
            _providerTokensQueries.DestroyAllAsync(cancellationToken),
            _providerTokens.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<SSOUserRoot>, Error>> FindByProviderUIdAsync(string providerName,
        string providerUId, CancellationToken cancellationToken)
    {
        var query = Query.From<SSOUser>()
            .Where<string>(usr => usr.ProviderUId, ConditionOperator.EqualTo, providerUId)
            .AndWhere<string>(usr => usr.ProviderName, ConditionOperator.EqualTo, providerName);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<SSOUserRoot>, Error>> FindByUserIdAsync(string providerName,
        Identifier userId, CancellationToken cancellationToken)
    {
        var query = Query.From<SSOUser>()
            .Where<string>(usr => usr.UserId, ConditionOperator.EqualTo, userId)
            .AndWhere<string>(usr => usr.ProviderName, ConditionOperator.EqualTo, providerName);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<ProviderAuthTokensRoot>, Error>> FindProviderTokensByUserIdAndProviderAsync(
        string providerName,
        Identifier userId, CancellationToken cancellationToken)
    {
        var query = Query.From<ProviderAuthTokens>()
            .Where<string>(toks => toks.UserId, ConditionOperator.EqualTo, userId)
            .AndWhere<string>(toks => toks.ProviderName, ConditionOperator.EqualTo, providerName);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<SSOUserRoot, Error>> SaveAsync(SSOUserRoot user, CancellationToken cancellationToken)
    {
        var saved = await _users.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return user;
    }

    public async Task<Result<ProviderAuthTokensRoot, Error>> SaveAsync(ProviderAuthTokensRoot authTokens,
        CancellationToken cancellationToken)
    {
        return await _providerTokens.UpsertAsync(authTokens, false, cancellationToken);
    }

    private async Task<Result<Optional<SSOUserRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<SSOUser> query, CancellationToken cancellationToken)
    {
        var queried = await _userQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<SSOUserRoot>.None;
        }

        var users = await _users.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (users.IsFailure)
        {
            return users.Error;
        }

        return users.Value.ToOptional();
    }

    private async Task<Result<Optional<ProviderAuthTokensRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<ProviderAuthTokens> query, CancellationToken cancellationToken)
    {
        var queried = await _providerTokensQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<ProviderAuthTokensRoot>.None;
        }

        var providerTokens = await _providerTokens.GetAsync(matching.Id.Value.ToId(), true, false, cancellationToken);
        if (providerTokens.IsFailure)
        {
            return providerTokens.Error;
        }

        return providerTokens.Value;
    }
}