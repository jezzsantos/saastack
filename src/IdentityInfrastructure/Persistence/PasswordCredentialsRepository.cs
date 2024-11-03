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
using MfaAuthenticator = IdentityApplication.Persistence.ReadModels.MfaAuthenticator;

namespace IdentityInfrastructure.Persistence;

public class PasswordCredentialsRepository : IPasswordCredentialsRepository
{
    private readonly ISnapshottingQueryStore<PasswordCredential> _credentialQueries;
    private readonly ISnapshottingQueryStore<MfaAuthenticator> _mfaAuthenticatorsQueries;
    private readonly IEventSourcingDddCommandStore<PasswordCredentialRoot> _credentials;

    public PasswordCredentialsRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<PasswordCredentialRoot> credentialsStore, IDataStore store)
    {
        _credentialQueries = new SnapshottingQueryStore<PasswordCredential>(recorder, domainFactory, store);
        _mfaAuthenticatorsQueries = new SnapshottingQueryStore<MfaAuthenticator>(recorder, domainFactory, store);
        _credentials = credentialsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _credentialQueries.DestroyAllAsync(cancellationToken),
            _mfaAuthenticatorsQueries.DestroyAllAsync(cancellationToken),
            _credentials.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByMfaAuthenticationTokenAsync(
        string token, CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.MfaAuthenticationToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByPasswordResetTokenAsync(
        string token, CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.PasswordResetToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>>
        FindCredentialsByRegistrationVerificationTokenAsync(string token,
            CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.RegistrationVerificationToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUsernameAsync(string username,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PasswordCredential>()
            .Where<string>(pc => pc.UserEmailAddress, ConditionOperator.EqualTo, username);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<PasswordCredentialRoot, Error>> SaveAsync(PasswordCredentialRoot credential,
        CancellationToken cancellationToken)
    {
        return await SaveAsync(credential, false, cancellationToken);
    }

    public async Task<Result<PasswordCredentialRoot, Error>> SaveAsync(PasswordCredentialRoot credential, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _credentials.SaveAsync(credential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(credential.Id, cancellationToken)
            : credential;
    }

    private async Task<Result<PasswordCredentialRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var credential = await _credentials.LoadAsync(id, cancellationToken);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        return credential;
    }

    private async Task<Result<Optional<PasswordCredentialRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<PasswordCredential> query,
        CancellationToken cancellationToken)
    {
        var queried = await _credentialQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<PasswordCredentialRoot>.None;
        }

        var credential = await _credentials.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        return credential.Value.ToOptional();
    }
}