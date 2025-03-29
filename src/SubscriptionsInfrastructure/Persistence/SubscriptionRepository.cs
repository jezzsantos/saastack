using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using SubscriptionsApplication.Persistence;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;
using Tasks = Common.Extensions.Tasks;

namespace SubscriptionsInfrastructure.Persistence;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ISnapshottingQueryStore<Subscription> _subscriptionQueries;
    private readonly IEventSourcingDddCommandStore<SubscriptionRoot> _subscriptions;

    public SubscriptionRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<SubscriptionRoot> subscriptionsStore, IDataStore store)
    {
        _subscriptionQueries = new SnapshottingQueryStore<Subscription>(recorder, domainFactory, store);
        _subscriptions = subscriptionsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _subscriptionQueries.DestroyAllAsync(cancellationToken),
            _subscriptions.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<SubscriptionRoot>, Error>> FindByBuyerReferenceAsync(string buyerReference,
        CancellationToken cancellationToken)
    {
        var query = Query.From<Subscription>()
            .Where<string>(at => at.BuyerReference, ConditionOperator.EqualTo, buyerReference);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<SubscriptionRoot>, Error>> FindByOwningEntityIdAsync(Identifier owningEntityId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<Subscription>()
            .Where<string>(at => at.OwningEntityId, ConditionOperator.EqualTo, owningEntityId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<SubscriptionRoot>, Error>> FindBySubscriptionReferenceAsync(
        string subscriptionReference, CancellationToken cancellationToken)
    {
        var query = Query.From<Subscription>()
            .Where<string>(at => at.SubscriptionReference, ConditionOperator.EqualTo, subscriptionReference);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<SubscriptionRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.LoadAsync(id, cancellationToken);
        if (subscription.IsFailure)
        {
            return subscription.Error;
        }

        return subscription;
    }

    public async Task<Result<SubscriptionRoot, Error>> SaveAsync(SubscriptionRoot subscription,
        CancellationToken cancellationToken)
    {
        var saved = await _subscriptions.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return subscription;
    }

    public async Task<Result<QueryResults<Subscription>, Error>> SearchAllByProviderAsync(string providerName,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var query = Query.From<Subscription>()
            .Where<string>(at => at.ProviderName, ConditionOperator.EqualTo, providerName)
            .WithSearchOptions(searchOptions);
        var queried = await _subscriptionQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value;
    }

    private async Task<Result<Optional<SubscriptionRoot>, Error>> FindFirstByQueryAsync(QueryClause<Subscription> query,
        CancellationToken cancellationToken)
    {
        var queried = await _subscriptionQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<SubscriptionRoot>.None;
        }

        var subscriptions = await _subscriptions.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (subscriptions.IsFailure)
        {
            return subscriptions.Error;
        }

        return subscriptions.Value.ToOptional();
    }
}