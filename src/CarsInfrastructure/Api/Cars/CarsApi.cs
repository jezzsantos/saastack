using Application.Interfaces;
using Application.Resources.Shared;
using CarsApplication;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Cars;

namespace CarsInfrastructure.Api.Cars;

public class CarsApi : IWebApiService
{
    private const string OrganizationId = "org_01234567890123456789012"; //TODO: get this from tenancy
    private readonly ICarsApplication _carsApplication;
    private readonly ICallerContext _context;

    public CarsApi(ICallerContext context, ICarsApplication carsApplication)
    {
        _context = context;
        _carsApplication = carsApplication;
    }

    public async Task<ApiDeleteResult> Delete(DeleteCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.DeleteCarAsync(_context, OrganizationId, request.Id, cancellationToken);
        return () => car.HandleApplicationResult();
    }

    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, OrganizationId, request.Id, cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }

    public async Task<ApiPostResult<Car, GetCarResponse>> Register(RegisterCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.RegisterCarAsync(_context, OrganizationId, request.Make, request.Model,
            request.Year, request.Jurisdiction, request.NumberPlate, cancellationToken);

        return () => car.HandleApplicationResult<GetCarResponse, Car>(c =>
            new PostResult<GetCarResponse>(new GetCarResponse { Car = c }, new GetCarRequest { Id = c.Id }.ToUrl()));
    }

    public async Task<ApiPutPatchResult<Car, GetCarResponse>> ScheduleMaintenance(ScheduleMaintenanceCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.ScheduleMaintenanceCarAsync(_context, OrganizationId, request.Id,
            request.FromUtc,
            request.ToUtc, cancellationToken);
        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }

    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAll(SearchAllCarsRequest request,
        CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllCarsAsync(_context, OrganizationId, request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse { Cars = c.Results, Metadata = c.Metadata });
    }

    public async Task<ApiSearchResult<Car, SearchAllCarsResponse>> SearchAllAvailable(
        SearchAllAvailableCarsRequest request, CancellationToken cancellationToken)
    {
        var cars = await _carsApplication.SearchAllAvailableCarsAsync(_context, OrganizationId, request.FromUtc,
            request.ToUtc, request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () =>
            cars.HandleApplicationResult(c => new SearchAllCarsResponse { Cars = c.Results, Metadata = c.Metadata });
    }

#if TESTINGONLY
    public async Task<ApiSearchResult<Unavailability, SearchAllCarUnavailabilitiesResponse>> SearchAllUnavailabilities(
        SearchAllCarUnavailabilitiesRequest request,
        CancellationToken cancellationToken)
    {
        var unavailabilities = await _carsApplication.SearchAllUnavailabilitiesAsync(_context, OrganizationId,
            request.Id,
            request.ToSearchOptions(),
            request.ToGetOptions(), cancellationToken);

        return () =>
            unavailabilities.HandleApplicationResult(c => new SearchAllCarUnavailabilitiesResponse
                { Unavailabilities = c.Results, Metadata = c.Metadata });
    }
#endif

    public async Task<ApiPutPatchResult<Car, GetCarResponse>> TakeOffline(TakeOfflineCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carsApplication.TakeOfflineCarAsync(_context, OrganizationId, request.Id,
            request.FromUtc, request.ToUtc, cancellationToken);
        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }
}