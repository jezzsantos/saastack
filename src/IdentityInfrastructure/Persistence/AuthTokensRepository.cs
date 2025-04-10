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

public class AuthTokensRepository : IAuthTokensRepository
{
    private readonly ISnapshottingQueryStore<AuthToken> _tokenQueries;
    private readonly ISnapshottingDddCommandStore<AuthTokensRoot> _tokens;

    public AuthTokensRepository(IRecorder recorder, IDomainFactory domainFactory,
        ISnapshottingDddCommandStore<AuthTokensRoot> tokensStore, IDataStore store)
    {
        _tokenQueries = new SnapshottingQueryStore<AuthToken>(recorder, domainFactory, store);
        _tokens = tokensStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _tokenQueries.DestroyAllAsync(cancellationToken),
            _tokens.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<AuthTokensRoot>, Error>> FindByRefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken)
    {
        var query = Query.From<AuthToken>()
            .Where<string>(at => at.RefreshToken, ConditionOperator.EqualTo, refreshToken);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<AuthTokensRoot>, Error>> FindByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<AuthToken>()
            .Where<string>(at => at.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<AuthTokensRoot, Error>> SaveAsync(AuthTokensRoot tokens,
        CancellationToken cancellationToken)
    {
        var saved = await _tokens.UpsertAsync(tokens, false, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return tokens;
    }

    private async Task<Result<Optional<AuthTokensRoot>, Error>> FindFirstByQueryAsync(QueryClause<AuthToken> query,
        CancellationToken cancellationToken)
    {
        var queried = await _tokenQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<AuthTokensRoot>.None;
        }

        var tokens = await _tokens.GetAsync(matching.Id.Value.ToId(), true, false, cancellationToken);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        return tokens.Value;
    }
}