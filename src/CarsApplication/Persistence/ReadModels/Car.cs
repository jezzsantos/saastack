using Application.Persistence.Common;
using CarsDomain;
using QueryAny;

namespace CarsApplication.Persistence.ReadModels;

[EntityName("Car")]
public class Car : ReadModelEntity
{
    public required string LicenseJurisdiction { get; set; }

    public required string LicenseNumber { get; set; }

    public required VehicleManagers ManagerIds { get; set; }

    public required string ManufactureMake { get; set; }

    public required string ManufactureModel { get; set; }

    public required int ManufactureYear { get; set; }

    public required string OrganisationId { get; set; }

    public required string Status { get; set; }

    public required string VehicleOwnerId { get; set; }
}