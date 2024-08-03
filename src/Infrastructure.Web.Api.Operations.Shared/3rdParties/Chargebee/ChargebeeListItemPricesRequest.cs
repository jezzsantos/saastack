using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/item_prices?prod_cat_ver=2#list_item_prices
/// </summary>
[Route("/item_prices", OperationMethod.Get)]
[UsedImplicitly]
public class
    ChargebeeListItemPricesRequest : UnTenantedRequest<ChargebeeListItemPricesRequest, ChargebeeListItemPricesResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }
}