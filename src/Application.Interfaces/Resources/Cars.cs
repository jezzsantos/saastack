namespace Application.Interfaces.Resources;

public class Car : IIdentifiableResource
{
    public required string Id { get; set; }

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required int Year { get; set; }
}