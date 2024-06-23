using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Exports all the subscriptions created by the currently installed Billing Provider,
///     that will need to be migrated by the next Billing Provider
/// </summary>
[Route("/subscriptions/export", OperationMethod.Search, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class ExportSubscriptionsToMigrateRequest : UnTenantedSearchRequest<ExportSubscriptionsToMigrateResponse>
{
}