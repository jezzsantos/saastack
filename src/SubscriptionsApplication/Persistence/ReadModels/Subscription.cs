using Application.Persistence.Common;
using Common;
using QueryAny;

namespace SubscriptionsApplication.Persistence.ReadModels;

[EntityName("Subscription")]
public class Subscription : ReadModelEntity
{
    public Optional<string> BuyerId { get; set; }

    public Optional<string> BuyerReference { get; set; }

    public Optional<string> OwningEntityId { get; set; }

    public Optional<string> ProviderName { get; set; }

    public Optional<string> ProviderState { get; set; }

    public Optional<string> SubscriptionReference { get; set; }
}