using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IEndUsersService
{
    Task<Result<Membership, Error>> CreateMembershipForCallerPrivateAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);

    Task<Result<Optional<EndUser>, Error>> FindPersonByEmailPrivateAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> GetMembershipsPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterMachinePrivateAsync(ICallerContext caller, string name,
        string? timezone,
        string? countryCode, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterPersonPrivateAsync(ICallerContext caller, string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken);
}