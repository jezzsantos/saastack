using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/invoices?prod_cat_ver=2#list_invoices
/// </summary>
[Route("/invoices", OperationMethod.Get)]
[UsedImplicitly]
public class ChargebeeListInvoicesRequest : UnTenantedRequest<ChargebeeListInvoicesResponse>
{
    public ChargebeeFilterQuery? Filter { get; set; }

    public int? Limit { get; set; }

    public ChargebeeSortBy? SortBy { get; set; }
}

[UsedImplicitly]
public class ChargebeeSortBy
{
}