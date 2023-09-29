using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[UsedImplicitly]
public class DeleteCarRequest : IWebRequestVoid
{
    public required string Id { get; set; }
}