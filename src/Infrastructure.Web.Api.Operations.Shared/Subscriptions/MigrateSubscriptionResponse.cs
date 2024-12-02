using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class MigrateSubscriptionResponse : IWebResponse
{
    public required Subscription Subscription { get; set; }
}