using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[UsedImplicitly]
public class GetCarRequest : IWebRequest<GetCarResponse>
{
    public required string Id { get; set; }
}