using Application.Interfaces;
using Application.Persistence.Common.Extensions;
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
using Tasks = Common.Extensions.Tasks;

namespace IdentityInfrastructure.Persistence;

public class OAuth2ClientRepository : IOAuth2ClientRepository
{
    private readonly ISnapshottingQueryStore<OAuth2Client> _clientQueries;
    private readonly IEventSourcingDddCommandStore<OAuth2ClientRoot> _clients;

    public OAuth2ClientRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OAuth2ClientRoot> clientsStore, IDataStore store)
    {
        _clientQueries = new SnapshottingQueryStore<OAuth2Client>(recorder, domainFactory, store);
        _clients = clientsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _clientQueries.DestroyAllAsync(cancellationToken),
            _clients.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<OAuth2ClientRoot>, Error>> FindById(Identifier id,
        CancellationToken cancellationToken)
    {
        var query = Query.From<OAuth2Client>()
            .Where<string>(client => client.Id, ConditionOperator.EqualTo, id);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<OAuth2ClientRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var client = await _clients.LoadAsync(id, cancellationToken);
        if (client.IsFailure)
        {
            return client.Error;
        }

        return client;
    }

    public async Task<Result<OAuth2ClientRoot, Error>> SaveAsync(OAuth2ClientRoot client,
        CancellationToken cancellationToken)
    {
        var saved = await _clients.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return client;
    }

    public async Task<Result<QueryResults<OAuth2Client>, Error>> SearchAllAsync(SearchOptions options,
        CancellationToken cancellationToken)
    {
        var query = Query.From<OAuth2Client>()
            .WhereAll()
            .WithSearchOptions(options);

        return await _clientQueries.QueryAsync(query, false, cancellationToken);
    }

    private async Task<Result<Optional<OAuth2ClientRoot>, Error>> FindFirstByQueryAsync(QueryClause<OAuth2Client> query,
        CancellationToken cancellationToken)
    {
        var queried = await _clientQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<OAuth2ClientRoot>.None;
        }

        var clients = await _clients.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (clients.IsFailure)
        {
            return clients.Error;
        }

        return clients.Value.ToOptional();
    }
}