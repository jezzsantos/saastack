using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Changes the billing subscription plan for the organization
/// </summary>
[Route("/subscriptions/{Id}/plan", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_BillingAdmin, Features.Tenant_Basic)]
public class ChangeSubscriptionPlanRequest : UnTenantedRequest<GetSubscriptionResponse>, IUnTenantedOrganizationRequest
{
    public string? PlanId { get; set; }

    public string? Id { get; set; }
}