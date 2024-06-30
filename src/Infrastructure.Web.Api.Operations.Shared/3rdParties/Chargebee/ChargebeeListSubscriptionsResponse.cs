using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

[UsedImplicitly]
public class ChargebeeListSubscriptionsResponse : IWebResponse
{
    public List<ChargebeeSubscriptionList>? List { get; set; }
}

[UsedImplicitly]
public class ChargebeeSubscriptionList
{
    public ChargebeeCustomer? Customer { get; set; }

    public ChargebeeSubscription? Subscription { get; set; }
}