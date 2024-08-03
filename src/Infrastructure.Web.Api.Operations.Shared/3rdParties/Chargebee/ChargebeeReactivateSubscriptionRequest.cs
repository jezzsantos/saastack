using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/subscriptions#reactivate_a_subscription
/// </summary>
[Route("/subscriptions/{Id}/reactivate", OperationMethod.Post)]
[UsedImplicitly]
public class ChargebeeReactivateSubscriptionRequest : UnTenantedRequest<ChargebeeReactivateSubscriptionRequest,
    ChargebeeReactivateSubscriptionResponse>
{
    public string? Id { get; set; }

    public long? TrialEnd { get; set; }
}