using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Migrates an existing subscription from the previously installed Billing Provider
///     to the currently installed Billing Provider
/// </summary>
[Route("/subscriptions/{Id}/migrate", OperationMethod.PutPatch, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class MigrateSubscriptionRequest : UnTenantedRequest<MigrateSubscriptionResponse>, IUnTenantedOrganizationRequest
{
    public string? ProviderName { get; set; }

    public Dictionary<string, string> ProviderState { get; set; } = new();

    public string? Id { get; set; }
}