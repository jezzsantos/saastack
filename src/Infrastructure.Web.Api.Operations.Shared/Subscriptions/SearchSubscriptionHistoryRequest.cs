using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Fetches a history of invoices for the subscription
/// </summary>
[Route("/subscriptions/{Id}/invoices", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Tenant_BillingAdmin, Features.Tenant_Basic)]
public class SearchSubscriptionHistoryRequest : UnTenantedSearchRequest<SearchSubscriptionHistoryResponse>,
    IUnTenantedOrganizationRequest
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public string? Id { get; set; }
}