using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Shared;
using UserProfilesDomain;

namespace UserProfilesApplication.Persistence;

public interface IUserProfileRepository : IApplicationRepository
{
    Task<Result<Optional<UserProfileRoot>, Error>> FindByEmailAddressAsync(EmailAddress emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Optional<UserProfileRoot>, Error>> FindByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<UserProfileRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<UserProfileRoot, Error>> SaveAsync(UserProfileRoot profile, CancellationToken cancellationToken);

    Task<Result<List<UserProfileRoot>, Error>> SearchAllByUserIdsAsync(List<Identifier> ids,
        CancellationToken cancellationToken);

    Task<Result<Optional<UserProfileRoot>, Error>> FindByAvatarIdAsync(Identifier avatarId,
        CancellationToken cancellationToken);
}