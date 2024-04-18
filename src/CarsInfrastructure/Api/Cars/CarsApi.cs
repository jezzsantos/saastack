using Application.Resources.Shared;
using CarsApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Cars;

namespace CarsInfrastructure.Api.Cars;

public sealed class CarsApi : IWebApiService
{
    private readonly ICarsApplication _carsApplication;
    private readonly ICallerContextFactory _contextFactory;

    public CarsApi(ICallerContextFactory contextFactory, ICarsApplication carsApplication)
    {
        _contextFactory = contextFactory;
        _carsApplication = carsApplication;
    }

    public async Task<ApiDeleteResult> Delete(DeleteCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.DeleteCarAsync(_contextFactory.Create(), request.OrganizationId!, request.Id,
            cancellationToken);
        return () => car.HandleApplicationResult();
    }

    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_contextFactory.Create(), request.OrganizationId!, request.Id,
            cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }

    public async Task<ApiPostResult<Car, GetCarResponse>> Register(RegisterCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.RegisterCarAsync(_contextFactory.Create(), request.OrganizationId!,
            request.Make, request.Model, request.Year, request.Jurisdiction, request.NumberPlate, cancellationToken);

        return () => car.HandleApplicationResult<Car, GetCarResponse>(c =>
            new PostResult<GetCarResponse>(new GetCarResponse { Car = c }, new GetCarRequest { Id = c.Id }.ToUrl()));
    }

    public async Task<ApiPutPatchResult<Car, GetCarResponse>> ScheduleMaintenance(ScheduleMaintenanceCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.ScheduleMaintenanceCarAsync(_contextFactory.Create(), request.OrganizationId!,
            request.Id, request.FromUtc, request.ToUtc, cancellationToken);
        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }

    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAll(SearchAllCarsRequest request,
        CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllCarsAsync(_contextFactory.Create(), request.OrganizationId!,
            request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse { Cars = c.Results, Metadata = c.Metadata });
    }

    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAllAvailable(
        SearchAllAvailableCarsRequest request, CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllAvailableCarsAsync(_contextFactory.Create(), request.OrganizationId!,
            request.FromUtc, request.ToUtc, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse { Cars = c.Results, Metadata = c.Metadata });
    }

#if TESTINGONLY
    public async Task<ApiSearchResult<Unavailability, SearchAllCarUnavailabilitiesResponse>> SearchAllUnavailabilities(
        SearchAllCarUnavailabilitiesRequest request,
        CancellationToken cancellationToken)
    {
        var unavailabilities = await _carsApplication.SearchAllUnavailabilitiesAsync(_contextFactory.Create(),
            request.OrganizationId!, request.Id, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            unavailabilities.HandleApplicationResult(c => new SearchAllCarUnavailabilitiesResponse
                { Unavailabilities = c.Results, Metadata = c.Metadata });
    }
#endif

    public async Task<ApiPutPatchResult<Car, GetCarResponse>> TakeOffline(TakeOfflineCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.TakeOfflineCarAsync(_contextFactory.Create(), request.OrganizationId!,
            request.Id, request.FromUtc, request.ToUtc, cancellationToken);
        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }
}