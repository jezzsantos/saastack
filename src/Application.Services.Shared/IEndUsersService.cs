using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IEndUsersService
{
    Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> GetPersonAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterMachineAsync(ICallerContext caller, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext caller, string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken);
}