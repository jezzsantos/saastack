using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using UserProfilesApplication.Persistence;
using UserProfilesApplication.Persistence.ReadModels;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Persistence;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ISnapshottingQueryStore<UserProfile> _profileQueries;
    private readonly IEventSourcingDddCommandStore<UserProfileRoot> _profiles;

    public UserProfileRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<UserProfileRoot> profilesStore, IDataStore store)
    {
        _profileQueries = new SnapshottingQueryStore<UserProfile>(recorder, domainFactory, store);
        _profiles = profilesStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _profileQueries.DestroyAllAsync(cancellationToken),
            _profiles.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<UserProfileRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var user = await _profiles.LoadAsync(id, cancellationToken);
        if (!user.IsSuccessful)
        {
            return user.Error;
        }

        return user;
    }

    public async Task<Result<UserProfileRoot, Error>> SaveAsync(UserProfileRoot profile,
        CancellationToken cancellationToken)
    {
        var saved = await _profiles.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        return profile;
    }

    public async Task<Result<List<UserProfileRoot>, Error>> SearchAllByUserIdsAsync(List<Identifier> ids,
        CancellationToken cancellationToken)
    {
        var tasks = await Task.WhenAll(ids.Select(async id => await FindByUserIdAsync(id, cancellationToken)));
        return tasks.ToList()
            .Where(task => task is { IsSuccessful: true, HasValue: true })
            .Select(task => task.Value.Value)
            .ToList();
    }

    public async Task<Result<Optional<UserProfileRoot>, Error>> FindByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<UserProfile>()
            .Where<string>(at => at.UserId, ConditionOperator.EqualTo, userId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<UserProfileRoot>, Error>> FindByEmailAddressAsync(EmailAddress emailAddress,
        CancellationToken cancellationToken)
    {
        var query = Query.From<UserProfile>()
            .Where<string>(at => at.EmailAddress, ConditionOperator.EqualTo, emailAddress.Address)
            .AndWhere<string>(at => at.Type, ConditionOperator.EqualTo, ProfileType.Person.ToString());
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    private async Task<Result<Optional<UserProfileRoot>, Error>> FindFirstByQueryAsync(QueryClause<UserProfile> query,
        CancellationToken cancellationToken)
    {
        var queried = await _profileQueries.QueryAsync(query, false, cancellationToken);
        if (!queried.IsSuccessful)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<UserProfileRoot>.None;
        }

        var profiles = await _profiles.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (!profiles.IsSuccessful)
        {
            return profiles.Error;
        }

        return profiles.Value.ToOptional();
    }
}