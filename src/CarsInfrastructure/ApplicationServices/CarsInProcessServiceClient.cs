using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using CarsApplication;
using Common;

namespace CarsInfrastructure.ApplicationServices;

/// <summary>
///     Provides an in-process service client to be used to make cross-domain calls,
///     when the Cars subdomain is deployed in the same host as the consumer of this service
/// </summary>
public class CarsInProcessServiceClient : ICarsService
{
    private readonly ICarsApplication _carsApplication;

    public CarsInProcessServiceClient(ICarsApplication carsApplication)
    {
        _carsApplication = carsApplication;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        return await _carsApplication.GetCarAsync(caller, organizationId, id, cancellationToken);
    }

    public async Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        return await _carsApplication.ReleaseCarAvailabilityAsync(caller, organizationId, id, fromUtc, toUtc,
            cancellationToken);
    }

    public async Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, string referenceId, CancellationToken cancellationToken)
    {
        return await _carsApplication.ReserveCarIfAvailableAsync(caller, organizationId, id, fromUtc, toUtc,
            referenceId, cancellationToken);
    }
}