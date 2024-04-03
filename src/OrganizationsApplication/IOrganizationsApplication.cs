using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Common;

namespace OrganizationsApplication;

public partial interface IOrganizationsApplication
{
    Task<Result<Error>> ChangeSettingsAsync(ICallerContext caller, string id,
        TenantSettings settings, CancellationToken cancellationToken);

    Task<Result<Organization, Error>> CreateSharedOrganizationAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken);

    Task<Result<Organization, Error>> GetOrganizationAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<OrganizationWithSettings, Error>> GetOrganizationSettingsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);
#endif

    Task<Result<TenantSettings, Error>> GetSettingsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<Organization, Error>> InviteMemberToOrganizationAsync(ICallerContext caller, string id, string? userId,
        string? emailAddress, CancellationToken cancellationToken);

    Task<Result<SearchResults<OrganizationMember>, Error>> ListMembersForOrganizationAsync(ICallerContext caller,
        string? id, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);
}