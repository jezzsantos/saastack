using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeListItemPricesResponse : IWebResponse
{
    public List<ChargebeeItemPriceList>? List { get; set; }
}

public class ChargebeeItemPriceList
{
    public ChargebeeItemPrice? ItemPrice { get; set; }
}

public class ChargebeeItemPrice
{
    public string? CurrencyCode { get; set; }

    public string? Description { get; set; }

    public string? ExternalName { get; set; }

    public int FreeQuantity { get; set; }

    public string? Id { get; set; }

    public string? ItemFamilyId { get; set; }

    public string? ItemId { get; set; }

    public string? ItemType { get; set; }

    public int Period { get; set; }

    public string? PeriodUnit { get; set; }

    public int Price { get; set; }

    public string? PricingModel { get; set; }

    public string? Status { get; set; }

    public int? TrialPeriod { get; set; }

    public string? TrialPeriodUnit { get; set; }
}