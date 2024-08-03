using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions?prod_cat_ver=1#retrieve_a_subscription
/// </summary>
[Route("/subscriptions/{Id}", OperationMethod.Get)]
[UsedImplicitly]
public class
    ChargebeeGetSubscriptionRequest : UnTenantedRequest<ChargebeeGetSubscriptionRequest,
    ChargebeeGetSubscriptionResponse>
{
    public string? Id { get; set; }
}