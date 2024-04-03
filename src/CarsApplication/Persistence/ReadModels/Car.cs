using Application.Persistence.Common;
using CarsDomain;
using Common;
using Domain.Shared.Cars;
using QueryAny;

namespace CarsApplication.Persistence.ReadModels;

[EntityName("Car")]
public class Car : ReadModelEntity
{
    public Optional<string> LicenseJurisdiction { get; set; }

    public Optional<string> LicenseNumber { get; set; }

    public VehicleManagers ManagerIds { get; set; } = VehicleManagers.Create();

    public Optional<string> ManufactureMake { get; set; }

    public Optional<string> ManufactureModel { get; set; }

    public Optional<int> ManufactureYear { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<CarStatus> Status { get; set; }

    public Optional<string> VehicleOwnerId { get; set; }
}