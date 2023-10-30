using Application.Interfaces;
using Application.Persistence.Interfaces;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Domain.Common.ValueObjects;

namespace CarsApplication.Persistence;

public interface ICarRepository : IApplicationRepository
{
    Task<Result<Error>> DeleteCarAsync(Identifier organizationId, Identifier id, CancellationToken cancellationToken);

    Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken);

    Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken);

    Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Car>, Error>> SearchAllAvailableCarsAsync(Identifier organizationId, DateTime from,
        DateTime to, SearchOptions searchOptions, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId, SearchOptions searchOptions,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Unavailability>, Error>> SearchAllCarUnavailabilitiesAsync(Identifier organizationId,
        Identifier id, SearchOptions searchOptions, CancellationToken cancellationToken);
}