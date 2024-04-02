using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using EndUsersApplication.Persistence;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using EndUser = EndUsersApplication.Persistence.ReadModels.EndUser;
using Tasks = Common.Extensions.Tasks;

namespace EndUsersInfrastructure.Persistence;

public class EndUserRepository : IEndUserRepository
{
    private readonly ISnapshottingQueryStore<MembershipJoinInvitation> _membershipUserQueries;
    private readonly ISnapshottingQueryStore<EndUser> _userQueries;
    private readonly IEventSourcingDddCommandStore<EndUserRoot> _users;

    public EndUserRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<EndUserRoot> usersStore, IDataStore store)
    {
        _userQueries = new SnapshottingQueryStore<EndUser>(recorder, domainFactory, store);
        _membershipUserQueries = new SnapshottingQueryStore<MembershipJoinInvitation>(recorder, domainFactory, store);
        _users = usersStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _userQueries.DestroyAllAsync(cancellationToken),
            _membershipUserQueries.DestroyAllAsync(cancellationToken),
            _users.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<EndUserRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var user = await _users.LoadAsync(id, cancellationToken);
        if (!user.IsSuccessful)
        {
            return user.Error;
        }

        return user;
    }

    public async Task<Result<EndUserRoot, Error>> SaveAsync(EndUserRoot user, CancellationToken cancellationToken)
    {
        var saved = await _users.SaveAsync(user, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        return user;
    }

    public async Task<Result<List<MembershipJoinInvitation>, Error>> SearchAllMembershipsByOrganizationAsync(
        Identifier organizationId, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var query = Query.From<MembershipJoinInvitation>()
            .Join<Invitation, string>(mje => mje.UserId, inv => inv.Id)
            .Where<string>(mje => mje.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .Select(mje => mje.UserId)
            .Select(mje => mje.Roles)
            .Select(mje => mje.Features)
            .Select(mje => mje.OrganizationId)
            .Select(mje => mje.IsDefault)
            .Select(mje => mje.LastPersistedAtUtc)
            .SelectFromJoin<Invitation, string>(mje => mje.InvitedEmailAddress, inv => inv.InvitedEmailAddress)
            .SelectFromJoin<Invitation, string>(mje => mje.Status, inv => inv.Status)
            .OrderBy(mje => mje.LastPersistedAtUtc)
            .WithSearchOptions(searchOptions);

        var queried = await _membershipUserQueries.QueryAsync(query, cancellationToken: cancellationToken);
        if (!queried.IsSuccessful)
        {
            return queried.Error;
        }

        var memberships = queried.Value.Results;
        return memberships;
    }
}