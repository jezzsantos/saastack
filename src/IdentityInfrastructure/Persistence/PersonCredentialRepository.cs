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

public class PersonCredentialRepository : IPersonCredentialRepository
{
    private readonly ISnapshottingQueryStore<PersonCredentialAuth> _credentialQueries;
    private readonly IEventSourcingDddCommandStore<PersonCredentialRoot> _credentials;
    private readonly ISnapshottingQueryStore<MfaAuthenticator> _mfaAuthenticatorsQueries;

    public PersonCredentialRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<PersonCredentialRoot> credentialsStore, IDataStore store)
    {
        _credentialQueries = new SnapshottingQueryStore<PersonCredentialAuth>(recorder, domainFactory, store);
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

    public async Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByMfaAuthenticationTokenAsync(
        string token, CancellationToken cancellationToken)
    {
        var query = Query.From<PersonCredentialAuth>()
            .Where<string>(pc => pc.MfaAuthenticationToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByPasswordResetTokenAsync(
        string token, CancellationToken cancellationToken)
    {
        var query = Query.From<PersonCredentialAuth>()
            .Where<string>(pc => pc.PasswordResetToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PersonCredentialAuth>()
            .Where<string>(pc => pc.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByUsernameAsync(string username,
        CancellationToken cancellationToken)
    {
        var query = Query.From<PersonCredentialAuth>()
            .Where<string>(pc => pc.UserEmailAddress, ConditionOperator.EqualTo, username);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<PersonCredentialRoot>, Error>>
        FindCredentialsByRegistrationVerificationTokenAsync(string token,
            CancellationToken cancellationToken)
    {
        var query = Query.From<PersonCredentialAuth>()
            .Where<string>(pc => pc.RegistrationVerificationToken, ConditionOperator.EqualTo, token);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<PersonCredentialRoot, Error>> SaveAsync(PersonCredentialRoot personCredential,
        CancellationToken cancellationToken)
    {
        return await SaveAsync(personCredential, false, cancellationToken);
    }

    public async Task<Result<PersonCredentialRoot, Error>> SaveAsync(PersonCredentialRoot personCredential, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _credentials.SaveAsync(personCredential, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(personCredential.Id, cancellationToken)
            : personCredential;
    }

    private async Task<Result<PersonCredentialRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var credential = await _credentials.LoadAsync(id, cancellationToken);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        return credential;
    }

    private async Task<Result<Optional<PersonCredentialRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<PersonCredentialAuth> query,
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
            return Optional<PersonCredentialRoot>.None;
        }

        var credential = await _credentials.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        return credential.Value.ToOptional();
    }
}