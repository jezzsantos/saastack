using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[UsedImplicitly]
public class TakeOfflineCarRequest : IWebRequest<GetCarResponse>
{
    public DateTime? EndAtUtc { get; set; }

    public string? Id { get; set; }

    public string? Reason { get; set; }

    public DateTime? StartAtUtc { get; set; }
}