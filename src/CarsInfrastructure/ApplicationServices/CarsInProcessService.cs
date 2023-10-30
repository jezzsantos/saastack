using Application.Interfaces;
using Application.Interfaces.Resources;
using Application.Interfaces.Services;
using CarsApplication;
using Common;

namespace CarsInfrastructure.ApplicationServices;

public class CarsInProcessService : ICarsService
{
    private readonly ICarsApplication _carsApplication;

    public CarsInProcessService(ICarsApplication carsApplication)
    {
        _carsApplication = carsApplication;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        return await _carsApplication.GetCarAsync(caller, organizationId, id, cancellationToken);
    }

    public async Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId,
        string id, DateTime startUtc,
        DateTime endUtc, CancellationToken cancellationToken)
    {
        return await _carsApplication.ReleaseCarAvailabilityAsync(caller, organizationId, id, startUtc, endUtc,
            cancellationToken);
    }

    public async Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId,
        string id, DateTime startUtc,
        DateTime endUtc, string referenceId, CancellationToken cancellationToken)
    {
        return await _carsApplication.ReserveCarIfAvailableAsync(caller, organizationId, id, startUtc, endUtc,
            referenceId, cancellationToken);
    }
}