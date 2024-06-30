using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Persistence.ReadModels;

public class SubscriptionProjection : IReadModelProjection
{
    private readonly IReadModelStore<Subscription> _subscriptions;

    public SubscriptionProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _subscriptions = new ReadModelStore<Subscription>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _subscriptions.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.OwningEntityId = e.OwningEntityId;
                        dto.BuyerId = e.BuyerId;
                        dto.ProviderName = e.ProviderName;
                    },
                    cancellationToken);

            case ProviderChanged e:
                return await _subscriptions.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.ProviderName = e.ToProviderName;
                    dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal);
                    dto.BuyerReference = e.BuyerReference;
                    dto.SubscriptionReference = e.SubscriptionReference;
                }, cancellationToken);

            case SubscriptionPlanChanged e:
                return await _subscriptions.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal);
                    dto.BuyerReference = e.BuyerReference;
                    dto.SubscriptionReference = e.SubscriptionReference;
                }, cancellationToken);

            case SubscriptionTransferred e:
                return await _subscriptions.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.BuyerId = e.ToBuyerId;
                    dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal);
                    dto.BuyerReference = e.BuyerReference;
                    dto.SubscriptionReference = e.SubscriptionReference;
                }, cancellationToken);

            case SubscriptionCanceled e:
                return await _subscriptions.HandleUpdateAsync(e.RootId,
                    dto => { dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal); },
                    cancellationToken);

            case SubscriptionUnsubscribed e:
                return await _subscriptions.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.SubscriptionReference = Optional<string>.None;
                        dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal);
                    },
                    cancellationToken);

            case PaymentMethodChanged e:
                return await _subscriptions.HandleUpdateAsync(e.RootId,
                    dto => { dto.ProviderState = e.ProviderState.ToJson(casing: StringExtensions.JsonCasing.Pascal); },
                    cancellationToken);

            case Deleted e:
                return await _subscriptions.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(SubscriptionRoot);
}