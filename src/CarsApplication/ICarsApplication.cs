using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;

namespace CarsApplication;

public interface ICarsApplication
{
    Task<Result<Error>> DeleteCarAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<Car, Error>> RegisterCarAsync(ICallerContext caller, string make, string model, int year,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<Car>, Error>> SearchAllCarsAsync(ICallerContext caller, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<Car, Error>> TakeOfflineCarAsync(ICallerContext caller, string id, string? reason, DateTime? startAtUtc,
        DateTime? endAtUtc, CancellationToken cancellationToken);
}