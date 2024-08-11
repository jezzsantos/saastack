using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Cancels the billing subscription for the organization
/// </summary>
[Route("/subscriptions/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_BillingAdmin, Features.Tenant_Basic)]
public class CancelSubscriptionRequest : UnTenantedRequest<CancelSubscriptionRequest, GetSubscriptionResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}