using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}/offline", ServiceOperation.PutPatch)]
public class TakeOfflineCarRequest : TenantedRequest<GetCarResponse>
{
    public DateTime? FromUtc { get; set; }

    public required string Id { get; set; }

    public DateTime? ToUtc { get; set; }
}