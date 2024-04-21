using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace EndUsersApplication;

public partial interface IEndUsersApplication
{
    Task<Result<EndUser, Error>> AssignPlatformRolesAsync(ICallerContext context, string id, List<string> roles,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> ChangeDefaultMembershipAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);

    Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAddressAsync(ICallerContext context, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext context, string id,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> GetUserAsync(ICallerContext context, string id, CancellationToken cancellationToken);

    Task<Result<SearchResults<Membership>, Error>> ListMembershipsForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterMachineAsync(ICallerContext context, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken);

    Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context, string? invitationToken,
        string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> UnassignPlatformRolesAsync(ICallerContext context, string id, List<string> roles,
        CancellationToken cancellationToken);
}