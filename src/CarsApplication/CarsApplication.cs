using Application.Common;
using Application.Interfaces;
using Application.Resources.Shared;
using CarsApplication.Persistence;
using CarsDomain;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;

namespace CarsApplication;

public class CarsApplication : ICarsApplication
{
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly ICarRepository _repository;

    public CarsApplication(IRecorder recorder, IIdentifierFactory idFactory, ICarRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _repository = repository;
    }

    public async Task<Result<Error>> DeleteCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        var deleted = car.Delete(caller.ToCallerId());
        if (!deleted.IsSuccessful)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Car {Id} was deleted", car.Id);
        return Result.Ok;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Car {Id} was fetched", car.Id);

        return car.ToCar();
    }

    public async Task<Result<Car, Error>> ScheduleMaintenanceCarAsync(ICallerContext caller, string organizationId,
        string id,
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.Exists)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        var timeSlot = TimeSlot.Create(fromUtc, toUtc);
        if (!timeSlot.IsSuccessful)
        {
            return timeSlot.Error;
        }

        var changed = car.ScheduleMaintenance(timeSlot.Value);
        if (!changed.IsSuccessful)
        {
            return changed.Error;
        }

        var updated = await _repository.SaveAsync(car, cancellationToken);
        return updated.Match<Result<Car, Error>>(c =>
        {
            _recorder.TraceInformation(caller.ToCall(), "Car {Id} was scheduled for maintenance from {From} until {To}",
                car.Id, fromUtc, toUtc);
            return c.Value.ToCar();
        }, error => error);
    }

    public async Task<Result<Car, Error>> RegisterCarAsync(ICallerContext caller, string organizationId, string make,
        string model, int year, string location, string plate, CancellationToken cancellationToken)
    {
        var retrieved = CarRoot.Create(_recorder, _idFactory, Identifier.Create(organizationId));
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        var manufacturer = Manufacturer.Create(year, make, model);
        if (!manufacturer.IsSuccessful)
        {
            return manufacturer.Error;
        }

        var manufactured = car.SetManufacturer(manufacturer.Value);
        if (!manufactured.IsSuccessful)
        {
            return manufactured.Error;
        }

        var ownerId = VehicleOwner.Create(caller.ToCallerId());
        if (!ownerId.IsSuccessful)
        {
            return ownerId.Error;
        }

        var ownership = car.SetOwnership(ownerId.Value);
        if (!ownership.IsSuccessful)
        {
            return ownership.Error;
        }

        var jurisdiction = Jurisdiction.Create(location);
        if (!jurisdiction.IsSuccessful)
        {
            return jurisdiction.Error;
        }

        var numberPlate = NumberPlate.Create(plate);
        if (!numberPlate.IsSuccessful)
        {
            return numberPlate.Error;
        }

        var license = LicensePlate.Create(jurisdiction.Value, numberPlate.Value);
        if (!license.IsSuccessful)
        {
            return license.Error;
        }

        var registration = car.ChangeRegistration(license.Value);
        if (!registration.IsSuccessful)
        {
            return registration.Error;
        }

        var updated = await _repository.SaveAsync(car, cancellationToken);
        return updated.Match<Result<Car, Error>>(c =>
        {
            _recorder.TraceInformation(caller.ToCall(), "Car {Id} was registered", c.Value.Id);
            return c.Value.ToCar();
        }, error => error);
    }

    public async Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.Exists)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        var slot = TimeSlot.Create(fromUtc, toUtc);
        if (!slot.IsSuccessful)
        {
            return slot.Error;
        }

        var released = car.ReleaseUnavailability(slot.Value);
        if (!released.IsSuccessful)
        {
            return released.Error;
        }

        var updated = await _repository.SaveAsync(car, cancellationToken);
        return updated.Match<Result<Car, Error>>(c =>
        {
            _recorder.TraceInformation(caller.ToCall(), "Car {Id} was made available from {From} until {To}",
                car.Id, fromUtc, toUtc);
            return c.Value.ToCar();
        }, error => error);
    }

    public async Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, string referenceId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.Exists)
        {
            return retrieved.Error;
        }

        var car = retrieved.Value;
        var slot = TimeSlot.Create(fromUtc, toUtc);
        if (!slot.IsSuccessful)
        {
            return slot.Error;
        }

        var availability = car.ReserveIfAvailable(slot.Value, referenceId);
        if (!availability.IsSuccessful)
        {
            return availability.Error;
        }

        var isAvailable = availability.Value;
        if (!isAvailable)
        {
            return false;
        }

        var updated = await _repository.SaveAsync(car, cancellationToken);
        return updated.Match<Result<bool, Error>>(_ =>
        {
            _recorder.TraceInformation(caller.ToCall(), "Car {Id} was reserved from {From} until {To}",
                car.Id, fromUtc, toUtc);
            return true;
        }, error => error);
    }

    public async Task<Result<SearchResults<Car>, Error>> SearchAllAvailableCarsAsync(ICallerContext caller,
        string organizationId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var searched = await _repository.SearchAllAvailableCarsAsync(organizationId.ToId(),
            fromUtc ?? DateTime.MinValue,
            toUtc ?? DateTime.MaxValue, searchOptions,
            cancellationToken);
        if (!searched.IsSuccessful)
        {
            return searched.Error;
        }

        var cars = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All available cars were fetched");

        return searchOptions.ApplyWithMetadata(cars.Select(car => car.ToCar()));
    }

    public async Task<Result<SearchResults<Car>, Error>> SearchAllCarsAsync(ICallerContext caller,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var searched = await _repository.SearchAllCarsAsync(organizationId.ToId(), searchOptions, cancellationToken);
        if (!searched.IsSuccessful)
        {
            return searched.Error;
        }

        var cars = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All cars were fetched");

        return searchOptions.ApplyWithMetadata(cars.Select(car => car.ToCar()));
    }

#if TESTINGONLY
    public async Task<Result<SearchResults<Unavailability>, Error>> SearchAllUnavailabilitiesAsync(
        ICallerContext caller, string organizationId, string carId, SearchOptions searchOptions,
        GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var searched = await _repository.SearchAllCarUnavailabilitiesAsync(organizationId.ToId(), carId.ToId(),
            searchOptions,
            cancellationToken);
        if (!searched.IsSuccessful)
        {
            return searched.Error;
        }

        var unavailabilities = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All unavailabilities for car {Id} were fetched", carId);

        return searchOptions.ApplyWithMetadata(
            unavailabilities.Select(unavailability => unavailability.ToUnavailability()));
    }
#endif

    public async Task<Result<Car, Error>> TakeOfflineCarAsync(ICallerContext caller, string organizationId, string id,
        DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!retrieved.Exists)
        {
            return Error.EntityNotFound();
        }

        var car = retrieved.Value;
        var timeSlot = TimeSlot.Create(fromUtc.GetValueOrDefault(), toUtc.GetValueOrDefault());
        if (!timeSlot.IsSuccessful)
        {
            return timeSlot.Error;
        }

        var changed = car.TakeOffline(timeSlot.Value);
        if (!changed.IsSuccessful)
        {
            return changed.Error;
        }

        var updated = await _repository.SaveAsync(car, cancellationToken);
        return updated.Match<Result<Car, Error>>(c =>
        {
            _recorder.TraceInformation(caller.ToCall(), "Car {Id} was taken offline", car.Id);
            return c.Value.ToCar();
        }, error => error);
    }
}

internal static class CarConversionExtensions
{
    public static Car ToCar(this CarRoot car)
    {
        return new Car
        {
            Id = car.Id,
            Owner = car.Owner.ToOwner().ValueOrDefault,
            Managers = car.Managers.ToManagers(),
            Status = car.Status.ToString(),
            Manufacturer = car.Manufacturer.ToManufacturer().ValueOrDefault,
            Plate = car.License.ToLicensePlate().ValueOrDefault
        };
    }

    public static Car ToCar(this Persistence.ReadModels.Car car)
    {
        return new Car
        {
            Id = car.Id,
            Owner = new CarOwner { Id = car.VehicleOwnerId },
            Managers = car.ManagerIds.Exists()
                ? car.ManagerIds.Managers.Select(id => new CarManager { Id = id }).ToList()
                : new List<CarManager>(),
            Manufacturer = new CarManufacturer
            {
                Year = car.ManufactureYear,
                Make = car.ManufactureMake,
                Model = car.ManufactureModel
            },
            Plate = new CarLicensePlate
                { Jurisdiction = car.LicenseJurisdiction, Number = car.LicenseNumber },
            Status = car.Status
        };
    }

#if TESTINGONLY
    public static Unavailability ToUnavailability(this Persistence.ReadModels.Unavailability unavailability)
    {
        return new Unavailability
        {
            Id = unavailability.Id,
            CarId = unavailability.CarId,
            CausedByReason = unavailability.CausedBy.ToString(),
            CausedByReference = unavailability.CausedByReference.ValueOrDefault
        };
    }
#endif

    private static Optional<CarManufacturer> ToManufacturer(this Optional<Manufacturer> manufacturer)
    {
        return manufacturer.HasValue
            ? new CarManufacturer
            {
                Year = manufacturer.Value.Year,
                Make = manufacturer.Value.Make,
                Model = manufacturer.Value.Model
            }
            : Optional<CarManufacturer>.None;
    }

    private static Optional<CarLicensePlate> ToLicensePlate(this Optional<LicensePlate> plate)
    {
        return plate.HasValue
            ? new CarLicensePlate
            {
                Jurisdiction = plate.Value.Jurisdiction,
                Number = plate.Value.Number
            }
            : Optional<CarLicensePlate>.None;
    }

    private static List<CarManager> ToManagers(this VehicleManagers managers)
    {
        return managers.HasValue()
            ? new List<CarManager>(managers.Managers.Select(id => new CarManager { Id = id }))
            : new List<CarManager>();
    }

    private static Optional<CarOwner> ToOwner(this Optional<VehicleOwner> owner)
    {
        return owner.HasValue
            ? new CarOwner { Id = owner.Value.OwnerId }
            : Optional<CarOwner>.None;
    }
}