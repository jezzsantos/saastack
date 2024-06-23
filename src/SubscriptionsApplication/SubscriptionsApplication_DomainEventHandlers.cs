using Application.Common.Extensions;
using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsApplication;

partial class SubscriptionsApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        return await CreateSubscriptionInternalAsync(caller, domainEvent.CreatedById.ToId(),
            domainEvent.RootId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken)
    {
        return await ForceDeleteSubscriptionForDeletedOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.DeletedById.ToId(), cancellationToken);
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

    private async Task<Result<Error>> CreateSubscriptionInternalAsync(ICallerContext caller, Identifier buyerId,
        Identifier owningEntityId, CancellationToken cancellationToken)
    {
        var created = SubscriptionRoot.Create(_recorder, _identifierFactory, owningEntityId, buyerId,
            _billingProvider.StateInterpreter);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var buyer = await CreateBuyerAsync(caller, buyerId, owningEntityId, cancellationToken);
        if (buyer.IsFailure)
        {
            return buyer.Error;
        }

        var subscribed = await _billingProvider.GatewayService.SubscribeAsync(caller, buyer.Value,
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

        var subscription = created.Value;
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

        _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} created for {BuyerId}", subscription.Id,
            subscription.BuyerId);

        return Result.Ok;
    }
}