using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace UserProfilesApplication;

public partial interface IUserProfilesApplication
{
    Task<Result<UserProfile, Error>> ChangeContactAddressAsync(ICallerContext caller, string userId, string? line1,
        string? line2,
        string? line3, string? city, string? state, string? countryCode, string? zipCode,
        CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> ChangeProfileAsync(ICallerContext caller, string userId, string? firstName,
        string? lastName,
        string? displayName, string? phoneNumber, string? timezone, CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> ChangeProfileAvatarAsync(ICallerContext caller, string userId, FileUpload upload,
        CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> DeleteProfileAvatarAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<List<UserProfile>, Error>> GetAllProfilesAsync(ICallerContext caller, List<string> ids,
        GetOptions options, CancellationToken cancellationToken);

    Task<Result<UserProfileForCaller, Error>> GetCurrentUserProfileAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<UserProfile, Error>> GetProfileAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);
}