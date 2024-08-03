using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions?prod_cat_ver=2#list_subscriptions
/// </summary>
[Route("/subscriptions", OperationMethod.Get)]
public class
    ChargebeeListSubscriptionsRequest : UnTenantedRequest<ChargebeeListSubscriptionsRequest,
    ChargebeeListSubscriptionsResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }
}