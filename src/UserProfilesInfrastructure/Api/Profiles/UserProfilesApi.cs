using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using Microsoft.AspNetCore.Http;
using UserProfilesApplication;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Api.Profiles;

public class UserProfilesApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IFileUploadService _fileUploadService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserProfilesApplication _userProfilesApplication;

    public UserProfilesApi(IHttpContextAccessor httpContextAccessor, IFileUploadService fileUploadService,
        ICallerContextFactory callerFactory, IUserProfilesApplication userProfilesApplication)
    {
        _httpContextAccessor = httpContextAccessor;
        _fileUploadService = fileUploadService;
        _callerFactory = callerFactory;
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<ApiResult<UserProfile, GetProfileResponse>> ChangeContactAddress(
        ChangeProfileContactAddressRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.ChangeContactAddressAsync(_callerFactory.Create(), request.UserId,
                request.Line1, request.Line2, request.Line3, request.City, request.State, request.CountryCode,
                request.Zip,
                cancellationToken);

        return () =>
            profile.HandleApplicationResult<UserProfile, GetProfileResponse>(pro => new GetProfileResponse
                { Profile = pro });
    }

    public async Task<ApiResult<UserProfile, GetProfileResponse>> ChangeProfile(
        ChangeProfileRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.ChangeProfileAsync(_callerFactory.Create(), request.UserId,
                request.FirstName, request.LastName, request.DisplayName, request.PhoneNumber, request.Timezone,
                cancellationToken);

        return () =>
            profile.HandleApplicationResult<UserProfile, GetProfileResponse>(pro => new GetProfileResponse
                { Profile = pro });
    }

    public async Task<ApiPutPatchResult<UserProfile, ChangeProfileAvatarResponse>> ChangeProfileAvatar(
        ChangeProfileAvatarRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = _httpContextAccessor.HttpContext!.Request;
        var uploaded = httpRequest.GetUploadedFile(_fileUploadService, Validations.Avatar.MaxSizeInBytes,
            Validations.Avatar.AllowableContentTypes);
        if (uploaded.IsFailure)
        {
            return () => uploaded.Error;
        }

        var profile =
            await _userProfilesApplication.ChangeProfileAvatarAsync(_callerFactory.Create(), request.UserId,
                uploaded.Value, cancellationToken);

        return () =>
            profile.HandleApplicationResult<UserProfile, ChangeProfileAvatarResponse>(pro =>
                new ChangeProfileAvatarResponse { Profile = pro });
    }

    public async Task<ApiResult<UserProfile, DeleteProfileAvatarResponse>> DeleteProfileAvatar(
        DeleteProfileAvatarRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.DeleteProfileAvatarAsync(_callerFactory.Create(), request.UserId,
                cancellationToken);

        return () =>
            profile.HandleApplicationResult<UserProfile, DeleteProfileAvatarResponse>(pro =>
                new DeleteProfileAvatarResponse { Profile = pro });
    }

    public async Task<ApiGetResult<UserProfileForCaller, GetProfileForCallerResponse>> GetProfileForCaller(
        GetProfileForCallerRequest request, CancellationToken cancellationToken)
    {
        var profile =
            await _userProfilesApplication.GetCurrentUserProfileAsync(_callerFactory.Create(), cancellationToken);

        return () =>
            profile.HandleApplicationResult<UserProfileForCaller, GetProfileForCallerResponse>(pro =>
                new GetProfileForCallerResponse { Profile = pro });
    }
}