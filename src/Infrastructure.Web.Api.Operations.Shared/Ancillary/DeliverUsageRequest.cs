using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/usages/deliver", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DeliverUsageRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}