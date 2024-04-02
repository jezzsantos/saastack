using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;

namespace EndUsersApplication.Persistence;

public interface IEndUserRepository : IApplicationRepository
{
    Task<Result<EndUserRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<EndUserRoot, Error>> SaveAsync(EndUserRoot user, CancellationToken cancellationToken);

    Task<Result<List<MembershipJoinInvitation>, Error>> SearchAllMembershipsByOrganizationAsync(
        Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken);
}