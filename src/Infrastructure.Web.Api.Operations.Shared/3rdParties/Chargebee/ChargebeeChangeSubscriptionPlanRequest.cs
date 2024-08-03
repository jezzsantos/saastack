using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions?prod_cat_ver=2#update_subscription_for_items
/// </summary>
[Route("/subscriptions/{Id}/update_for_items", OperationMethod.Post)]
[UsedImplicitly]
public class ChargebeeChangeSubscriptionPlanRequest : UnTenantedRequest<ChargebeeChangeSubscriptionPlanRequest,
    ChargebeeChangeSubscriptionPlanResponse>
{
    public string? Id { get; set; }

    public bool ReplaceItemsList { get; set; }

    public List<ChargebeeSubscriptionItem> SubscriptionItems { get; set; } = new();
}

public class ChargebeeSubscriptionItem
{
    public decimal Amount { get; set; }

    public string? ItemPriceId { get; set; }

    public string? ItemType { get; set; }

    public int Quantity { get; set; }

    public long? TrialEnd { get; set; }

    public decimal UnitPrice { get; set; }
}