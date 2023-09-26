using Application.Interfaces;
using Application.Interfaces.Resources;
using CarsApplication;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.Cars;

namespace CarsApi.Apis.Cars;

public class CarsApi : IWebApiService
{
    private readonly ICarsApplication _carsApplication;
    private readonly ICallerContext _context;

    public CarsApi(ICallerContext context, ICarsApplication carsApplication)
    {
        _context = context;
        _carsApplication = carsApplication;
    }

    [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
    public async Task<ApiResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_context, request.Id, cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }
}