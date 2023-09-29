using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[UsedImplicitly]
public class RegisterCarRequest : IWebRequest<GetCarResponse>
{
    public required string Make { get; set; }

    public required string Model { get; set; }

    public required int Year { get; set; }
}