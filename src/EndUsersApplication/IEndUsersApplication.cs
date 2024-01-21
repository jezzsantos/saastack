using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace EndUsersApplication;

public interface IEndUsersApplication
{
    Task<Result<EndUser, Error>> AssignPlatformRolesAsync(ICallerContext context, string id, List<string> roles,
        CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> AssignTenantRolesAsync(ICallerContext context, string organizationId,
        string id,
        List<string> roles, CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext context, string id,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> GetPersonAsync(ICallerContext context, string id, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterMachineAsync(ICallerContext context, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context, string emailAddress,
        string firstName, string lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken);
}