using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions#remove_scheduled_cancellation
/// </summary>
[Route("/subscriptions/{Id}/remove_scheduled_cancellation", OperationMethod.Post)]
[UsedImplicitly]
public class
    ChargebeeRemoveScheduledCancellationSubscriptionRequest : UnTenantedRequest<
    ChargebeeRemoveScheduledCancellationSubscriptionRequest, ChargebeeRemoveScheduledCancellationSubscriptionResponse>
{
    public string? Id { get; set; }
}