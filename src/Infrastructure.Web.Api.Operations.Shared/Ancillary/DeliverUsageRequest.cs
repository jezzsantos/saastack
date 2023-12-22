using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/usages/deliver", ServiceOperation.Post)]
public class DeliverUsageRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}