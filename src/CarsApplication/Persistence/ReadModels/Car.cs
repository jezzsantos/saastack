using Application.Persistence.Interfaces;

namespace CarsApplication.Persistence.ReadModels;

public class Car : IReadModelEntity
{
    public string Id { get; set; } = null!;

    public string? Make { get; set; }

    public string? Model { get; set; }

    public int? Year { get; set; }
}