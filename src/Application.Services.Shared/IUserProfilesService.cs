using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IUserProfilesService
{
    Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressPrivateAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken);

    Task<Result<List<UserProfile>, Error>> GetAllProfilesPrivateAsync(ICallerContext caller, List<string> ids,
        GetOptions options, CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> GetProfilePrivateAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);
}