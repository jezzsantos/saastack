using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace CarsApplication;

public interface ICarsApplication
{
    Task<Result<Error>> DeleteCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken);

    Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken);

    Task<Result<Car, Error>> RegisterCarAsync(ICallerContext caller, string organizationId, string make, string model,
        int year, string location, string plate, CancellationToken cancellationToken);

    Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, string referenceId, CancellationToken cancellationToken);

    Task<Result<Car, Error>> ScheduleMaintenanceCarAsync(ICallerContext caller, string organizationId, string id,
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<Result<SearchResults<Car>, Error>> SearchAllAvailableCarsAsync(ICallerContext caller, string organizationId,
        DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<Car>, Error>> SearchAllCarsAsync(ICallerContext caller, string organizationId,
        SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<SearchResults<Unavailability>, Error>> SearchAllUnavailabilitiesAsync(ICallerContext caller,
        string organizationId, string carId, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);
#endif

    Task<Result<Car, Error>> TakeOfflineCarAsync(ICallerContext caller, string organizationId, string id,
        DateTime? fromUtc,
        DateTime? toUtc, CancellationToken cancellationToken);
}