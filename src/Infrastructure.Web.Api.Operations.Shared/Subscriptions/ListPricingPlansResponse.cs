using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class ListPricingPlansResponse : IWebResponse
{
    public PricingPlans? Plans { get; set; }
}