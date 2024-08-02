using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;

namespace SubscriptionsApplication.Persistence;

public interface ISubscriptionRepository : IApplicationRepository
{
    Task<Result<Optional<SubscriptionRoot>, Error>> FindByBuyerReferenceAsync(string buyerReference,
        CancellationToken cancellationToken);

    Task<Result<Optional<SubscriptionRoot>, Error>> FindByOwningEntityIdAsync(Identifier owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<Optional<SubscriptionRoot>, Error>> FindBySubscriptionReferenceAsync(string subscriptionReference,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<SubscriptionRoot, Error>> SaveAsync(SubscriptionRoot subscription, CancellationToken cancellationToken);

    Task<Result<List<Subscription>, Error>> SearchAllByProviderAsync(string providerName,
        SearchOptions searchOptions, CancellationToken cancellationToken);
}