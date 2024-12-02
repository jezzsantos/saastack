using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class ListPricingPlansResponse : IWebResponse
{
    public required PricingPlans Plans { get; set; }
}