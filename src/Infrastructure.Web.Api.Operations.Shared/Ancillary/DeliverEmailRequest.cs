using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/emails/deliver", ServiceOperation.Post, AccessType.HMAC)]
public class DeliverEmailRequest : UnTenantedRequest<DeliverMessageResponse>
{
    public required string Message { get; set; }
}