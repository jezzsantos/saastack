using Application.Interfaces;
using CarsApplication.Persistence;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.ValueObjects;

namespace CarsInfrastructure.Persistence;

//TODO: stop using this in-memory repo and move to persistence layer
public class CarRepository : ICarRepository
{
    private static readonly List<CarRoot> Cars = new();

    private static readonly List<Unavailability> Unavailabilities = new();

    public async Task<Result<Error>> DestroyAllAsync()
    {
        await Task.CompletedTask;

        Cars.Clear();
        Unavailabilities.Clear();

        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteCarAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var car = Cars.Find(root => root.Id == id);
        if (car.NotExists())
        {
            return Error.EntityNotFound();
        }

        Cars.Remove(car);

        return Result.Ok;
    }

    public async Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var car = Cars.Find(root => root.Id == id);
        if (car.NotExists())
        {
            return Error.EntityNotFound();
        }

        return car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var existing = Cars.Find(root => root.Id == car.Id);
        if (existing.NotExists())
        {
            Cars.Add(car);
            AddRemoveAvailabilities(car);
            return car;
        }

        AddRemoveAvailabilities(car);
        existing = car;
        return existing;

        static void AddRemoveAvailabilities(CarRoot car)
        {
            var events = car.GetChanges().Value;
            foreach (var @event in events)
            {
                if (@event.EventType == nameof(CarsDomain.Events.Car.UnavailabilitySlotAdded))
                {
                    var domainEvent =
                        (CarsDomain.Events.Car.UnavailabilitySlotAdded)@event.Data.FromEventJson(
                            typeof(CarsDomain.Events.Car.UnavailabilitySlotAdded));
                    var unavailability = Unavailabilities.Find(una => una.Id == domainEvent.UnavailabilityId);
                    if (unavailability.NotExists())
                    {
                        Unavailabilities.Add(new Unavailability
                        {
                            Id = domainEvent.UnavailabilityId!,
                            IsDeleted = null,
                            LastPersistedAtUtc = DateTime.UtcNow,
                            CarId = car.Id,
                            CausedBy = domainEvent.CausedByReason,
                            CausedByReference = domainEvent.CausedByReference,
                            From = domainEvent.From,
                            OrganisationId = domainEvent.OrganizationId,
                            To = domainEvent.To
                        });
                    }
                }

                if (@event.EventType == nameof(CarsDomain.Events.Car.UnavailabilitySlotRemoved))
                {
                    var domainEvent =
                        (CarsDomain.Events.Car.UnavailabilitySlotRemoved)@event.Data.FromEventJson(
                            typeof(CarsDomain.Events.Car.UnavailabilitySlotRemoved));
                    var unavailability = Unavailabilities.Find(una => una.Id == domainEvent.UnavailabilityId);
                    if (unavailability.Exists())
                    {
                        Unavailabilities.Remove(unavailability);
                    }
                }
            }
        }
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken)
    {
        return await SaveAsync(car, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllAvailableCarsAsync(Identifier organizationId,
        DateTime from, DateTime to, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var unavailableCarIds = Unavailabilities
            .FindAll(unavailability => unavailability.From >= from && unavailability.To <= to)
            .Select(unavailability => unavailability.CarId)
            .Distinct();

        var allCarsIds = Cars.Select(car => car.Id.ToString());

        return allCarsIds.Except(unavailableCarIds)
            .Select(carId => Cars.First(car => car.Id == carId).ToCar())
            .ToList();
    }

    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Cars.Select(car => car.ToCar())
            .ToList();
    }

    public async Task<Result<IReadOnlyList<Unavailability>, Error>> SearchAllCarUnavailabilitiesAsync(
        Identifier organizationId, Identifier id, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Unavailabilities
            .Where(unavailability => unavailability.CarId == id)
            .ToList();
    }
}

internal static class RepositoryExtensions
{
    public static Car ToCar(this CarRoot car)
    {
        return new Car
        {
            Id = car.Id,
            OrganisationId = car.OrganizationId,
            ManufactureMake = (car.Manufacturer.Exists()
                ? car.Manufacturer.Make
                : null)!,
            ManufactureModel = (car.Manufacturer.Exists()
                ? car.Manufacturer.Model
                : null)!,
            ManufactureYear = (car.Manufacturer.Exists()
                ? car.Manufacturer.Year
                : null)!,
            LicenseJurisdiction = car.License!.Jurisdiction,
            LicenseNumber = car.License.Number,
            ManagerIds = car.Managers,
            Status = car.Status.ToString(),
            VehicleOwnerId = car.Owner?.OwnerId!
        };
    }
}