using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace EndUsersInfrastructure.Persistence;

public class InvitationRepository : IInvitationRepository
{
    private readonly ISnapshottingQueryStore<Invitation> _invitationQueries;
    private readonly IEventSourcingDddCommandStore<EndUserRoot> _users;

    public InvitationRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<EndUserRoot> usersStore, IDataStore store)
    {
        _invitationQueries = new SnapshottingQueryStore<Invitation>(recorder, domainFactory, store);
        _users = usersStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _invitationQueries.DestroyAllAsync(cancellationToken),
            _users.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<EndUserRoot>, Error>> FindInvitedGuestByEmailAddressAsync(
        EmailAddress emailAddress, CancellationToken cancellationToken)
    {
        var query = Query.From<Invitation>()
            .Where<string>(eu => eu.InvitedEmailAddress, ConditionOperator.EqualTo, emailAddress.Address)
            .AndWhere<UserStatus>(eu => eu.Status, ConditionOperator.EqualTo, UserStatus.Unregistered);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<EndUserRoot>, Error>> FindInvitedGuestByTokenAsync(string token,
        CancellationToken cancellationToken)
    {
        var query = Query.From<Invitation>()
            .Where<string>(eu => eu.Token, ConditionOperator.EqualTo, token)
            .AndWhere<UserStatus>(eu => eu.Status, ConditionOperator.EqualTo, UserStatus.Unregistered);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<EndUserRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var user = await _users.LoadAsync(id, cancellationToken);
        if (user.IsFailure)
        {
            return user.Error;
        }

        return user;
    }

    public async Task<Result<EndUserRoot, Error>> SaveAsync(EndUserRoot user, CancellationToken cancellationToken)
    {
        var saved = await _users.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return user;
    }

    private async Task<Result<Optional<EndUserRoot>, Error>> FindFirstByQueryAsync(QueryClause<Invitation> query,
        CancellationToken cancellationToken)
    {
        var queried = await _invitationQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<EndUserRoot>.None;
        }

        var users = await _users.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (users.IsFailure)
        {
            return users.Error;
        }

        return users.Value.ToOptional();
    }
}