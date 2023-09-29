using Application.Interfaces;
using Application.Persistence.Interfaces;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;

namespace CarsApplication.Persistence;

public interface ICarRepository : IApplicationRepository
{
    Task<Result<None, Error>> DeleteCarAsync(string id);

    Task<Result<CarRoot, Error>> GetCarAsync(string id);

    Task<Result<CarRoot, Error>> Save(CarRoot car);

    Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(SearchOptions searchOptions, GetOptions getOptions);
}