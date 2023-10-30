using Application.Interfaces.Resources;
using Common;

namespace Application.Interfaces.Services;

public interface ICarsService
{
    Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken);

    Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId, string id,
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken);

    Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId, string id,
        DateTime startUtc, DateTime endUtc, string referenceId, CancellationToken cancellationToken);
}