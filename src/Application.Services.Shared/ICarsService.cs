using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface ICarsService
{
    Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken);

    Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId, string id,
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId, string id,
        DateTime fromUtc, DateTime toUtc, string referenceId, CancellationToken cancellationToken);
}