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

public class PasswordCredentialsRepository : IPasswordCredentialsRepository
{
    private readonly ISnapshottingQueryStore<PasswordCredential> _credentialQueries;
    private readonly IEventSourcingDddCommandStore<PasswordCredentialRoot> _credentials;

    public PasswordCredentialsRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<PasswordCredentialRoot> credentialsStore, IDataStore store)
    {
        _credentialQueries = new SnapshottingQueryStore<PasswordCredential>(recorder, domainFactory, store);
        _credentials = credentialsStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _credentialQueries.DestroyAllAsync(cancellationToken),
            _credentials.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByTokenAsync(string token,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.RegistrationVerificationToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUsernameAsync(string username,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.UserEmailAddress, ConditionOperator.EqualTo, username);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<PasswordCredentialRoot, Error>> SaveAsync(PasswordCredentialRoot credential,
        CancellationToken cancellationToken)
    {
        var saved = await _credentials.SaveAsync(credential, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        return credential;
    }

    private async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<PasswordCredential> query,
        CancellationToken cancellationToken)
    {
        var queried = await _credentialQueries.QueryAsync(query, false, cancellationToken);
        if (!queried.IsSuccessful)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<PasswordCredentialRoot>.None;
        }

        var tokens = await _credentials.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (!tokens.IsSuccessful)
        {
            return tokens.Error;
        }

        return tokens.Value.ToOptional();
    }
}