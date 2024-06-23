using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class ExportSubscriptionsToMigrateResponse : SearchResponse
{
    public List<SubscriptionToMigrate>? Subscriptions { get; set; }
}