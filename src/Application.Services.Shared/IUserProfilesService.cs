using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IUserProfilesService
{
    Task<Result<UserProfile, Error>> CreateMachineProfilePrivateAsync(ICallerContext caller, string machineId,
        string name,
        string? timezone, string? countryCode, CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> CreatePersonProfilePrivateAsync(ICallerContext caller, string personId,
        string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, CancellationToken cancellationToken);

    Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressPrivateAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken);
}