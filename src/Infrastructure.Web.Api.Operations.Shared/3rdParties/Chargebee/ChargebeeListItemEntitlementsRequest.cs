using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API:
///     https://apidocs.chargebee.com/docs/api/item_entitlements?prod_cat_ver=2#list_item_entitlements_for_an_item
/// </summary>
[Route("/items/{PlanId}/item_entitlements", OperationMethod.Get)]
[UsedImplicitly]
public class ChargebeeListItemEntitlementsRequest : UnTenantedRequest<ChargebeeListItemEntitlementsRequest,
    ChargebeeListItemEntitlementsResponse>
{
    public int? Limit { get; set; }

    public string? PlanId { get; set; }
}