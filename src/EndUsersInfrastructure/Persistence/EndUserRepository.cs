using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using EndUsersApplication.Persistence;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace EndUsersInfrastructure.Persistence;

public class EndUserRepository : IEndUserRepository
{
    private readonly ISnapshottingQueryStore<EndUser> _userQueries;
    private readonly IEventSourcingDddCommandStore<EndUserRoot> _users;

    public EndUserRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<EndUserRoot> usersStore, IDataStore store)
    {
        _userQueries = new SnapshottingQueryStore<EndUser>(recorder, domainFactory, store);
        _users = usersStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _userQueries.DestroyAllAsync(cancellationToken),
            _users.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<EndUserRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var audit = await _users.LoadAsync(id, cancellationToken);
        if (!audit.IsSuccessful)
        {
            return audit.Error;
        }

        return audit;
    }

    public async Task<Result<EndUserRoot, Error>> SaveAsync(EndUserRoot endUser, CancellationToken cancellationToken)
    {
        await _users.SaveAsync(endUser, cancellationToken);

        return endUser;
    }
}