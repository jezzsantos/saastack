using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using UserProfilesApplication;

namespace UserProfilesInfrastructure.Api;

public class UserProfilesApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IUserProfilesApplication _userProfilesApplication;

    public UserProfilesApi(ICallerContextFactory contextFactory, IUserProfilesApplication userProfilesApplication)
    {
        _contextFactory = contextFactory;
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<ApiResult<UserProfile, GetProfileResponse>> ChangeContactAddress(
        ChangeProfileContactAddressRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.ChangeContactAddressAsync(_contextFactory.Create(), request.UserId,
                request.Line1, request.Line2, request.Line3, request.City, request.State, request.CountryCode,
                request.Zip,
                cancellationToken);

        return () =>
            profile.HandleApplicationResult<GetProfileResponse, UserProfile>(pro => new GetProfileResponse
                { Profile = pro });
    }

    public async Task<ApiResult<UserProfile, GetProfileResponse>> ChangeProfile(
        ChangeProfileRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.ChangeProfileAsync(_contextFactory.Create(), request.UserId,
                request.FirstName, request.LastName, request.DisplayName, request.PhoneNumber, request.Timezone,
                cancellationToken);

        return () =>
            profile.HandleApplicationResult<GetProfileResponse, UserProfile>(pro => new GetProfileResponse
                { Profile = pro });
    }
}