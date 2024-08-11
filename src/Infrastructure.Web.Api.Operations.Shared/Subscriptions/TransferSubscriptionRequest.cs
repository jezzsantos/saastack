using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Transfers the subscription to another Billing Admin, who will become the subscription buyer
/// </summary>
[Route("/subscriptions/{Id}/transfer", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_BillingAdmin, Features.Tenant_Basic)]
public class TransferSubscriptionRequest : UnTenantedRequest<TransferSubscriptionRequest, GetSubscriptionResponse>,
    IUnTenantedOrganizationRequest
{
    public string? UserId { get; set; }

    public string? Id { get; set; }
}