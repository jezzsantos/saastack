using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using CarsApplication.Persistence;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = Common.Extensions.Task;

namespace CarsInfrastructure.Persistence;

public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;
    private readonly ISnapshottingQueryStore<Unavailability> _unavailabilitiesQueries;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
        _unavailabilitiesQueries = new SnapshottingQueryStore<Unavailability>(recorder, domainFactory, store);
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Task.WhenAllAsync(
            _carQueries.DestroyAllAsync(cancellationToken),
            _cars.DestroyAllAsync(cancellationToken),
            _unavailabilitiesQueries.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var car = await _cars.LoadAsync(id, cancellationToken);
        if (!car.IsSuccessful)
        {
            return car.Error;
        }

        return car.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken)
    {
        await _cars.SaveAsync(car, cancellationToken);

        return reload
            ? await LoadAsync(car.OrganizationId, car.Id, cancellationToken)
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken)
    {
        return await SaveAsync(car, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllAvailableCarsAsync(Identifier organizationId,
        DateTime from, DateTime to, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var unavailabilities = await _unavailabilitiesQueries.QueryAsync(Query.From<Unavailability>()
                .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
                .AndWhere<DateTime>(u => u.From, ConditionOperator.LessThanEqualTo, from)
                .AndWhere<DateTime>(u => u.To, ConditionOperator.GreaterThanEqualTo, to),
            cancellationToken: cancellationToken);
        if (!unavailabilities.IsSuccessful)
        {
            return unavailabilities.Error;
        }

        var limit = searchOptions.Limit;
        var offset = searchOptions.Offset;
        searchOptions.ClearLimitAndOffset();

        var cars = await _carQueries.QueryAsync(Query.From<Car>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .AndWhere<string>(c => c.Status, ConditionOperator.EqualTo, CarStatus.Registered.ToString())
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (!cars.IsSuccessful)
        {
            return cars.Error;
        }

        return cars.Value.Results
            .Where(car => unavailabilities.Value.Results.All(unavailability => unavailability.CarId != car.Id))
            .Skip(offset)
            .Take(limit)
            .ToList();
    }

    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var cars = await _carQueries.QueryAsync(Query.From<Car>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (!cars.IsSuccessful)
        {
            return cars.Error;
        }

        return cars.Value.Results;
    }

    public async Task<Result<IReadOnlyList<Unavailability>, Error>> SearchAllCarUnavailabilitiesAsync(
        Identifier organizationId, Identifier id, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var unavailabilities = await _unavailabilitiesQueries.QueryAsync(Query.From<Unavailability>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .AndWhere<string>(u => u.CarId, ConditionOperator.EqualTo, id)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (!unavailabilities.IsSuccessful)
        {
            return unavailabilities.Error;
        }

        return unavailabilities.Value.Results;
    }
}