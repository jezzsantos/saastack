using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace CarsDomain;

public class CarRoot : IIdentifiableEntity
{
    public CarRoot(IIdentifierFactory idFactory)
    {
        Id = idFactory.Create(this);
    }

    public Identifier Id { get; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public int? Year { get; set; }
}