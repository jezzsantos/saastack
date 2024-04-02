using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using UserProfilesApplication;

namespace UserProfilesInfrastructure.ApplicationServices;

public class UserProfilesInProcessServiceClient : IUserProfilesService
{
    private readonly IUserProfilesApplication _userProfilesApplication;

    public UserProfilesInProcessServiceClient(IUserProfilesApplication userProfilesApplication)
    {
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<Result<UserProfile, Error>> CreateMachineProfilePrivateAsync(ICallerContext caller,
        string machineId, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken)
    {
        return await _userProfilesApplication.CreateProfileAsync(caller, UserProfileClassification.Machine, machineId,
            null, name,
            null, timezone, countryCode, cancellationToken);
    }

    public async Task<Result<UserProfile, Error>> CreatePersonProfilePrivateAsync(ICallerContext caller,
        string personId, string emailAddress, string firstName,
        string? lastName, string? timezone, string? countryCode, CancellationToken cancellationToken)
    {
        return await _userProfilesApplication.CreateProfileAsync(caller, UserProfileClassification.Person, personId,
            emailAddress,
            firstName, lastName, timezone, countryCode, cancellationToken);
    }

    public async Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressPrivateAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        return await _userProfilesApplication.FindPersonByEmailAddressAsync(caller, emailAddress, cancellationToken);
    }

    public async Task<Result<List<UserProfile>, Error>> GetAllProfilesPrivateAsync(ICallerContext caller,
        List<string> ids, GetOptions options, CancellationToken cancellationToken)
    {
        return await _userProfilesApplication.GetAllProfilesAsync(caller, ids, options, cancellationToken);
    }

    public async Task<Result<UserProfile, Error>> GetProfilePrivateAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        return await _userProfilesApplication.GetProfileAsync(caller, userId, cancellationToken);
    }
}