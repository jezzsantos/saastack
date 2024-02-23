using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IOrganizationsService
{
    Task<Result<Error>> ChangeSettingsPrivateAsync(ICallerContext caller, string id, TenantSettings settings,
        CancellationToken cancellationToken);

    Task<Result<Organization, Error>> CreateOrganizationPrivateAsync(ICallerContext caller, string creatorId,
        string name, OrganizationOwnership ownership, CancellationToken cancellationToken);

    Task<Result<TenantSettings, Error>> GetSettingsPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);
}