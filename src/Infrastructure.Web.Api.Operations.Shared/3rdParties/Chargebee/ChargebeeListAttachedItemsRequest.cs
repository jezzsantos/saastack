using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/attached_items?prod_cat_ver=2#list_attached_items
/// </summary>
[Route("/items/{PlanId}/attached_items", OperationMethod.Get)]
[UsedImplicitly]
public class ChargebeeListAttachedItemsRequest : UnTenantedRequest<ChargebeeListAttachedItemsResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }

    public string? PlanId { get; set; }
}

[UsedImplicitly]
public class ChargebeeFilterQuery
{
}