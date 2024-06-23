using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     List all the pricing plans available for this product
/// </summary>
[Route("/pricing/plans", OperationMethod.Get)]
public class ListPricingPlansRequest : UnTenantedRequest<ListPricingPlansResponse>
{
}