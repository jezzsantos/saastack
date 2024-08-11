using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Fetches the billing subscription for the organization
/// </summary>
[Route("/subscriptions/{Id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Tenant_BillingAdmin, Features.Tenant_Basic)]
public class GetSubscriptionRequest : UnTenantedRequest<GetSubscriptionRequest, GetSubscriptionResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}