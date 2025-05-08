using Application.Common.Extensions;
using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared.Subscriptions;
using SubscriptionsDomain;
using Created = Domain.Events.Shared.UserProfiles.Created;

namespace SubscriptionsApplication;

partial class SubscriptionsApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller,
        Domain.Events.Shared.Organizations.Created domainEvent, CancellationToken cancellationToken)
    {
        return await CreateSubscriptionInternalAsync(caller, domainEvent.CreatedById.ToId(), domainEvent.RootId.ToId(),
            cancellationToken);
    }

    public async Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken)
    {
        return await ForceDeleteSubscriptionForDeletedOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.DeletedById.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleUserProfileCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        return await CreateSubscriptionInternalAsync(caller, domainEvent.UserId.ToId(), Optional<Identifier>.None,
            cancellationToken);
    }

    /// <summary>
    ///     To create a subscription in full we need both and existing organization (OwningEntity),
    ///     and an existing buyer (UserProfile).
    ///     The organization is created separately from the profile, and these events happen in parallel,
    ///     in response to other events, and so we have to wait for both events to occur before we can "complete" the
    ///     subscription. A "complete" subscription being one that has both an OwningEntity and a Buyer and a Provider.
    ///     Thus, we have to react to either event arriving first, and then wait for the other event to arrive
    ///     to complete the process, and we have to respond differently depending on which event arrives
    ///     first and which event arrives second. They can also arrive at pretty much the same time too.
    ///     Note: If we receive the <see cref="Domain.Events.UserProfiles.Created" /> event first, we can do nothing. Since,
    ///     Not all Users associated to a newly created UserProfile are going to be Buyers of a subscription.
    ///     Note: If we receive the <see cref="Domain.Events.Organizations.Created" /> event first, we can create a "partial"
    ///     subscription if the UserProfile does not exist yet. Or, we can create the "complete" subscription if the
    ///     UserProfile does exist at that time.
    ///     Note: If we receive the <see cref="Domain.Events.UserProfiles.Created" /> event second, and there is an
    ///     associated "partial" subscription for this Buyer already, then we can complete the subscription.
    ///     Note: If we receive the <see cref="Domain.Events.Organizations.Created" /> event second, and the Buyer is found,
    ///     then we can "complete" the subscription.
    /// </summary>
    private async Task<Result<Error>> CreateSubscriptionInternalAsync(ICallerContext caller, Identifier buyerId,
        Optional<Identifier> owningEntityId, CancellationToken cancellationToken)
    {
        if (IsOrganizationCreatedEvent())
        {
            var created = SubscriptionRoot.Create(_recorder, _identifierFactory, owningEntityId.Value, buyerId,
                _billingProvider.StateInterpreter);
            if (created.IsFailure)
            {
                return created.Error;
            }

            var subscription = created.Value;
            var buyer = await CreateBuyerAsync(caller, buyerId, owningEntityId, cancellationToken);
            if (buyer.IsFailure)
            {
                return buyer.Error;
            }

            if (buyer.Value.HasValue)
            {
                return await CompleteSubscriptionAsync(subscription, buyer.Value.Value);
            }

            return await SavePartialSubscriptionAsync(subscription);
        }

        if (IsUserProfileCreatedEvent())
        {
            var retrievedSubscription =
                await _repository.FindByBuyerIdAsync(buyerId, cancellationToken);
            if (retrievedSubscription.IsFailure)
            {
                return retrievedSubscription.Error;
            }

            if (retrievedSubscription.Value.HasValue)
            {
                var subscription = retrievedSubscription.Value.Value;
                if (subscription.IsCompleted)
                {
                    _recorder.TraceInformation(caller.ToCall(),
                        "Subscription {Id} has already been completed for {BuyerId}", subscription.Id,
                        subscription.BuyerId);
                    return Result.Ok;
                }

                var buyer = await CreateBuyerAsync(caller, buyerId, subscription.OwningEntityId, cancellationToken);
                if (buyer.IsFailure)
                {
                    return buyer.Error;
                }

                if (buyer.Value.HasValue)
                {
                    return await CompleteSubscriptionAsync(subscription, buyer.Value.Value);
                }
            }
        }

        return Result.Ok;

        async Task<Result<Error>> CompleteSubscriptionAsync(SubscriptionRoot subscription, SubscriptionBuyer buyer)
        {
            var subscribed = await _billingProvider.GatewayService.SubscribeAsync(caller, buyer,
                SubscribeOptions.Immediately, cancellationToken);
            if (subscribed.IsFailure)
            {
                return subscribed.Error;
            }

            var provider = BillingProvider.Create(_billingProvider.ProviderName, subscribed.Value);
            if (provider.IsFailure)
            {
                return provider.Error;
            }

            var provided = subscription.SetProvider(provider.Value, buyerId, _billingProvider.StateInterpreter);
            if (provided.IsFailure)
            {
                return provided.Error;
            }

            var saved = await _repository.SaveAsync(subscription, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} created complete for {BuyerId}",
                subscription.Id,
                subscription.BuyerId);

            return Result.Ok;
        }

        async Task<Result<Error>> SavePartialSubscriptionAsync(SubscriptionRoot subscription)
        {
            var saved = await _repository.SaveAsync(subscription, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} created partially for {BuyerId}",
                subscription.Id,
                subscription.BuyerId);

            return Result.Ok;
        }

        bool IsOrganizationCreatedEvent()
        {
            return owningEntityId.HasValue;
        }

        bool IsUserProfileCreatedEvent()
        {
            return !owningEntityId.HasValue;
        }
    }

    private async Task<Result<Error>> ForceDeleteSubscriptionForDeletedOrganizationAsync(ICallerContext caller,
        Identifier owningEntityId, Identifier deleterId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var deleted = subscription.DeleteSubscription(deleterId, owningEntityId);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} deleted", subscription.Id);

        return Result.Ok;
    }
}