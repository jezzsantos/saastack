using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions?prod_cat_ver=2#cancel_subscription_for_items
/// </summary>
[Route("/subscriptions/{Id}/cancel_for_items", OperationMethod.Post)]
public class ChargebeeCancelSubscriptionRequest : UnTenantedRequest<ChargebeeCancelSubscriptionRequest,
    ChargebeeCancelSubscriptionResponse>
{
    public long? CancelAt { get; set; }

    public bool EndOfTerm { get; set; }

    public string? Id { get; set; }
}