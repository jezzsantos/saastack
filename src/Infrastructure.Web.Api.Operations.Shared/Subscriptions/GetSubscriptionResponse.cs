using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class GetSubscriptionResponse : IWebResponse
{
    public required SubscriptionWithPlan Subscription { get; set; }
}