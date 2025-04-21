using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Forces the billing subscription to be canceled for the organization.
/// </summary>
[Route("/subscriptions/{Id}/force", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class ForceCancelSubscriptionRequest : UnTenantedRequest<ForceCancelSubscriptionRequest, GetSubscriptionResponse>
{
    public string? Id { get; set; }
}