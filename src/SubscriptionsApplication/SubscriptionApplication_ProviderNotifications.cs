using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Domain.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsApplication;

partial class SubscriptionsApplication
{
    /// <summary>
    ///     Returns the subscription associated to the buyer reference.
    ///     Note: There should only be one subscription for each buyer reference.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> GetProviderStateForBuyerAsync(ICallerContext caller,
        string buyerReference, CancellationToken cancellationToken)
    {
        var retrievedSubscription =
            await _repository.FindByBuyerReferenceAsync(buyerReference, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var subscription = retrievedSubscription.Value.Value;
        if (!subscription.Provider.HasValue)
        {
            return Error.EntityNotFound();
        }

        return subscription.Provider.Value.State;
    }

    /// <summary>
    ///     Returns the subscription associated to the subscription reference.
    ///     Note: There should only be one subscription for each subscription reference.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> GetProviderStateForSubscriptionAsync(ICallerContext caller,
        string subscriptionReference, CancellationToken cancellationToken)
    {
        var retrievedSubscription =
            await _repository.FindBySubscriptionReferenceAsync(subscriptionReference, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var subscription = retrievedSubscription.Value.Value;
        if (!subscription.Provider.HasValue)
        {
            return Error.EntityNotFound();
        }

        return subscription.Provider.Value.State;
    }

    public async Task<Result<Error>> NotifyBuyerDeletedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var provider = BillingProvider.Create(providerName, state);
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var retrievedSubscription =
            await FindSubscriptionByBuyerReference(provider.Value, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Result.Ok;
        }

        var subscription = retrievedSubscription.Value.Value;
        var deleted = await subscription.RestoreBuyerAfterDeletedFromProviderAsync(
            _billingProvider.StateInterpreter, caller.ToCallerId(), provider.Value, OnRestoreBuyer);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} had its buyer deleted by the provider, and restored", subscription.Id);

        return Result.Ok;

        async Task<Result<SubscriptionMetadata, Error>> OnRestoreBuyer(SubscriptionRoot subscription1)
        {
            var buyer = await CreateBuyerAsync(caller, subscription1.BuyerId, subscription1.OwningEntityId,
                cancellationToken);
            if (buyer.IsFailure)
            {
                return buyer.Error;
            }

            var restoredBuyer = await _billingProvider.GatewayService.RestoreBuyerAsync(caller,
                buyer.Value, cancellationToken);
            if (restoredBuyer.IsFailure)
            {
                return restoredBuyer.Error;
            }

            return restoredBuyer.Value;
        }
    }

    public async Task<Result<Error>> NotifyBuyerPaymentMethodChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var provider = BillingProvider.Create(providerName, state);
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var retrievedSubscription =
            await FindSubscriptionByBuyerReference(provider.Value, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Result.Ok;
        }

        var subscription = retrievedSubscription.Value.Value;
        var changed = subscription.ChangePaymentMethodForBuyerFromProvider(
            _billingProvider.StateInterpreter, caller.ToCallerId(), provider.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} had its buyer payment method changed by the provider", subscription.Id);

        return Result.Ok;
    }

    public async Task<Result<Error>> NotifySubscriptionCancelledAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var provider = BillingProvider.Create(providerName, state);
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var retrievedSubscription =
            await FindSubscriptionBySubscriptionReference(provider.Value, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Result.Ok;
        }

        var subscription = retrievedSubscription.Value.Value;
        var cancellerId = caller.ToCallerId();
        var canceled =
            subscription.CancelSubscriptionFromProvider(_billingProvider.StateInterpreter, cancellerId, provider.Value);
        if (canceled.IsFailure)
        {
            return canceled.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} was cancelled by the provider",
            subscription.Id);

        return Result.Ok;
    }

    public async Task<Result<Error>> NotifySubscriptionDeletedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var provider = BillingProvider.Create(providerName, state);
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var retrievedSubscription =
            await FindSubscriptionBySubscriptionReference(provider.Value, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Result.Ok;
        }

        var subscription = retrievedSubscription.Value.Value;
        var deleterId = caller.ToCallerId();
        var deleted = subscription.DeleteSubscriptionFromProvider(_billingProvider.StateInterpreter,
            deleterId, provider.Value);
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
        _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} was deleted by the provider",
            subscription.Id);

        return Result.Ok;
    }

    public async Task<Result<Error>> NotifySubscriptionPlanChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var provider = BillingProvider.Create(providerName, state);
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var retrievedSubscription =
            await FindSubscriptionBySubscriptionReference(provider.Value, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Result.Ok;
        }

        var subscription = retrievedSubscription.Value.Value;
        var modifierId = caller.ToCallerId();
        var changed =
            subscription.ChangeSubscriptionPlanFromProvider(_billingProvider.StateInterpreter, modifierId,
                provider.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} had its plan changed by the provider",
            subscription.Id);

        return Result.Ok;
    }

    private async Task<Result<Optional<SubscriptionRoot>, Error>> FindSubscriptionBySubscriptionReference(
        BillingProvider provider, CancellationToken cancellationToken)
    {
        var subscriptionReference = _billingProvider.StateInterpreter.GetSubscriptionReference(provider);
        if (subscriptionReference.IsFailure)
        {
            return subscriptionReference.Error;
        }

        // Assumption: there can be only one subscription for the same subscription reference
        var retrievedSubscriptions =
            await _repository.FindBySubscriptionReferenceAsync(subscriptionReference.Value, cancellationToken);
        if (retrievedSubscriptions.IsFailure)
        {
            return retrievedSubscriptions.Error;
        }

        return retrievedSubscriptions.Value;
    }

    private async Task<Result<Optional<SubscriptionRoot>, Error>> FindSubscriptionByBuyerReference(
        BillingProvider provider, CancellationToken cancellationToken)
    {
        var buyerReference = _billingProvider.StateInterpreter.GetBuyerReference(provider);
        if (buyerReference.IsFailure)
        {
            return buyerReference.Error;
        }

        // Assumption: there can be only one subscription for the same buyer reference
        var retrievedSubscriptions =
            await _repository.FindByBuyerReferenceAsync(buyerReference.Value, cancellationToken);
        if (retrievedSubscriptions.IsFailure)
        {
            return retrievedSubscriptions.Error;
        }

        return retrievedSubscriptions.Value;
    }
}