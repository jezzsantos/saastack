namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

[Route("/cars/{id}/offline", ServiceOperation.PutPatch)]
public class TakeOfflineCarRequest : TenantedRequest<GetCarResponse>
{
    public DateTime? FromUtc { get; set; }

    public required string Id { get; set; }

    public DateTime? ToUtc { get; set; }
}