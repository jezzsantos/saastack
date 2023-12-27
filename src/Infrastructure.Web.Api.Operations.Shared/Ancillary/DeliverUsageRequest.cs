using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/usages/deliver", ServiceOperation.Post, AccessType.HMAC)]
public class DeliverUsageRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}