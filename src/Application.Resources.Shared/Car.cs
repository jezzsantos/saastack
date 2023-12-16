using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Car : IIdentifiableResource
{
    public required List<CarManager>? Managers { get; set; }

    public required CarManufacturer? Manufacturer { get; set; }

    public required CarOwner? Owner { get; set; }

    public required CarLicensePlate? Plate { get; set; }

    public required string Status { get; set; }

    public required string Id { get; set; }
}

public class CarManager
{
    public required string Id { get; set; }
}

public class CarOwner
{
    public required string Id { get; set; }
}

public class CarLicensePlate
{
    public required string Jurisdiction { get; set; }

    public required string Number { get; set; }
}

public class CarManufacturer
{
    public required string Make { get; set; }

    public required string Model { get; set; }

    public required int Year { get; set; }
}

public class CarModel
{
    public required string Make { get; set; }

    public required string Model { get; set; }
}

public class Unavailability : IIdentifiableResource
{
    public required string CarId { get; set; }

    public required string CausedByReason { get; set; }

    public string? CausedByReference { get; set; }

    public required string Id { get; set; }
}