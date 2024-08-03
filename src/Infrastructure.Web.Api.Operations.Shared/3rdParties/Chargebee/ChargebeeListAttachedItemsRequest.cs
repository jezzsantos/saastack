using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/attached_items?prod_cat_ver=2#list_attached_items
/// </summary>
[Route("/items/{PlanId}/attached_items", OperationMethod.Get)]
[UsedImplicitly]
public class
    ChargebeeListAttachedItemsRequest : UnTenantedRequest<ChargebeeListAttachedItemsRequest,
    ChargebeeListAttachedItemsResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }

    public string? PlanId { get; set; }
}

[UsedImplicitly]
public class ChargebeeFilterQuery : IParsable<ChargebeeFilterQuery>
{
    public IChargeBeeFilterQuery? In { get; set; }

    public IChargeBeeFilterQuery? Is { get; set; }

    static ChargebeeFilterQuery IParsable<ChargebeeFilterQuery>.Parse(string s, IFormatProvider? provider)
    {
        return new ChargebeeFilterQuery();
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ChargebeeFilterQuery result)
    {
        result = new ChargebeeFilterQuery();
        return true;
    }
}

public interface IChargeBeeFilterQuery
{
    string Status { get; set; }
}