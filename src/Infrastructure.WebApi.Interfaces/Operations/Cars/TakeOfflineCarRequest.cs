namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[Route("/cars/{id}/offline", ServiceOperation.PutPatch)]
public class TakeOfflineCarRequest : IWebRequest<GetCarResponse>
{
    public DateTime? EndAtUtc { get; set; }

    public string? Id { get; set; }

    public string? Reason { get; set; }

    public DateTime? StartAtUtc { get; set; }
}