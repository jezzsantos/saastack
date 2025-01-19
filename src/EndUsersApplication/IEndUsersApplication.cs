using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace EndUsersApplication;

public partial interface IEndUsersApplication
{
    Task<Result<EndUser, Error>> AssignPlatformRolesAsync(ICallerContext caller, string id, List<string> roles,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> ChangeDefaultMembershipAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> GetUserAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<SearchResults<Membership>, Error>> ListMembershipsForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> RegisterMachineAsync(ICallerContext caller, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken);

    Task<Result<EndUserWithProfile, Error>> RegisterPersonAsync(ICallerContext caller, string? invitationToken,
        string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> UnassignPlatformRolesAsync(ICallerContext caller, string id, List<string> roles,
        CancellationToken cancellationToken);
}