using CarsApplication;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using Microsoft.AspNetCore.Http;

namespace CarsApi.Apis.Cars;

public class CarsApi : IWebApiService
{
    private readonly ICarsApplication _carsApplication;

    public CarsApi(ICarsApplication carsApplication)
    {
        _carsApplication = carsApplication;
    }

    [WebApiRoute("/cars/{id}", WebApiOperation.Get)]
    public async Task<IResult> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(new CallerContext(), request.Id, cancellationToken);
        return Results.Ok(new GetCarResponse { Car = car });
    }
}