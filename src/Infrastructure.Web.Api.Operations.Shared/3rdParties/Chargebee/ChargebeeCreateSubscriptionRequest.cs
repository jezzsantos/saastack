using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions?prod_cat_ver=2#create_subscription_for_items
/// </summary>
[Route("/customers/{CustomerId}/subscription_for_items", OperationMethod.Post)]
public class ChargebeeCreateSubscriptionRequest : UnTenantedRequest<ChargebeeCreateSubscriptionRequest,
    ChargebeeCreateSubscriptionResponse>
{
    public string? AutoCollection { get; set; }

    public string? CustomerId { get; set; }

    public string? Id { get; set; }

    public string? MetaData { get; set; }

    public long? StartDate { get; set; }

    public List<ChargebeeSubscriptionItem> SubscriptionItems { get; set; } = new();
}