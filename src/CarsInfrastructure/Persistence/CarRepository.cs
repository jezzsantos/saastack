using Application.Interfaces;
using CarsApplication.Persistence;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Common.Extensions;

namespace CarsInfrastructure.Persistence;

public class CarRepository : ICarRepository
{
    private static readonly List<CarRoot> Cars = new();

    public async Task<Result<CarRoot, Error>> GetCarAsync(string id)
    {
        await Task.CompletedTask;

        var car = Cars.Find(root => root.Id == id);
        if (car.NotExists())
        {
            return new Result<CarRoot, Error>(Error.EntityNotFound());
        }

        return car!;
    }

    public async Task<Result<None, Error>> DeleteCarAsync(string id)
    {
        await Task.CompletedTask;

        var car = Cars.Find(root => root.Id == id);
        if (car.NotExists())
        {
            return new Result<None, Error>(Error.EntityNotFound());
        }

        Cars.Remove(car!);

        return new None();
    }

    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(SearchOptions searchOptions,
        GetOptions getOptions)
    {
        await Task.CompletedTask;

        return new Result<IReadOnlyList<Car>, Error>((IReadOnlyList<Car>)Cars.Select(car => car.ToCar()).ToList());
    }

    public async Task<Result<CarRoot, Error>> Save(CarRoot car)
    {
        await Task.CompletedTask;

        var existing = Cars.Find(root => root.Id == car.Id);
        if (existing.Exists())
        {
            existing = car;
            return existing;
        }

        Cars.Add(car);

        return car;
    }

    public void DestroyAll()
    {
        Cars.Clear();
    }
}

internal static class RepositoryExtensions
{
    public static Car ToCar(this CarRoot car)
    {
        return new Car
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Year = car.Year
        };
    }
}