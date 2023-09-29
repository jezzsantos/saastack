using Application.Interfaces;
using Application.Interfaces.Resources;
using CarsApplication.Persistence;
using CarsDomain;
using Common;
using Domain.Interfaces.Entities;

namespace CarsApplication;

public class CarsApplication : ICarsApplication
{
    private readonly IIdentifierFactory _idFactory;
    private readonly ICarRepository _repository;

    public CarsApplication(IIdentifierFactory idFactory, ICarRepository repository)
    {
        _idFactory = idFactory;
        _repository = repository;
    }

    public async Task<Result<Error>> DeleteCarAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var car = await _repository.GetCarAsync(id);
        if (!car.Exists)
        {
            return Error.EntityNotFound();
        }

        await _repository.DeleteCarAsync(car.Value.Id);

        return Result.Ok;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var car = await _repository.GetCarAsync(id);
        if (!car.Exists)
        {
            return Error.EntityNotFound();
        }

        return car.Value.ToCar();
    }

    public async Task<Result<Car, Error>> RegisterCarAsync(ICallerContext caller, string make, string model, int year,
        CancellationToken cancellationToken)
    {
        var car = new CarRoot(_idFactory);

        var created = await _repository.Save(car);

        return created.Match<Result<Car, Error>>(c => c.Value.ToCar(), error => error);
    }

    public async Task<Result<SearchResults<Car>, Error>> SearchAllCarsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var cars = await _repository.SearchAllCarsAsync(searchOptions, getOptions);
        if (!cars.Exists)
        {
            return Error.EntityNotFound();
        }

        return searchOptions.ApplyWithMetadata(cars.Value.Select(car => car.ToCar()));
    }

    public async Task<Result<Car, Error>> TakeOfflineCarAsync(ICallerContext caller, string id, string? reason,
        DateTime? startAtUtc, DateTime? endAtUtc, CancellationToken cancellationToken)
    {
        var car = await _repository.GetCarAsync(id);
        if (!car.Exists)
        {
            return Error.EntityNotFound();
        }

        //TODO change the state of the root
        var updated = await _repository.Save(car.Value);

        return updated.Value.ToCar();
    }
}

internal static class CarConversionExtensions
{
    public static Car ToCar(this CarRoot car)
    {
        return new Car
        {
            Id = car.Id,
            Make = "amake",
            Model = "amodel",
            Year = 2023
        };
    }

    public static Car ToCar(this Persistence.ReadModels.Car car)
    {
        return new Car
        {
            Id = car.Id,
            Make = "amake",
            Model = "amodel",
            Year = 2023
        };
    }
}