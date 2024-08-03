using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/invoices?prod_cat_ver=2#list_invoices
/// </summary>
[Route("/invoices", OperationMethod.Get)]
[UsedImplicitly]
public class
    ChargebeeListInvoicesRequest : UnTenantedRequest<ChargebeeListInvoicesRequest, ChargebeeListInvoicesResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }

    public ChargebeeSortBy? SortBy { get; set; }
}

[UsedImplicitly]
public class ChargebeeSortBy : IParsable<ChargebeeSortBy>
{
    public ChargebeeSortByField By { get; set; } //values: date, updated_at

    public ChargebeeSortByOrder Order { get; set; } //values: asc, desc

    public static ChargebeeSortBy Parse(string s, IFormatProvider? provider)
    {
        return new ChargebeeSortBy
        {
            By = ChargebeeSortByField.Date,
            Order = ChargebeeSortByOrder.Asc
        };
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ChargebeeSortBy result)
    {
        result = new ChargebeeSortBy
        {
            By = ChargebeeSortByField.Date,
            Order = ChargebeeSortByOrder.Asc
        };
        return true;
    }
}

public enum ChargebeeSortByField
{
    Date,
    UpdatedAt
}

public enum ChargebeeSortByOrder
{
    Asc,
    Desc
}