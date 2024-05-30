using Application.Resources.Shared;
using EndUsersApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.EndUsers;

public class EndUsersApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IEndUsersApplication _endUsersApplication;

    public EndUsersApi(ICallerContextFactory callerFactory, IEndUsersApplication endUsersApplication)
    {
        _callerFactory = callerFactory;
        _endUsersApplication = endUsersApplication;
    }

    public async Task<ApiPostResult<EndUser, UpdateUserResponse>> AssignPlatformRoles(
        AssignPlatformRolesRequest request, CancellationToken cancellationToken)
    {
        var user =
            await _endUsersApplication.AssignPlatformRolesAsync(_callerFactory.Create(), request.Id!,
                request.Roles ?? new List<string>(), cancellationToken);

        return () => user.HandleApplicationResult<EndUser, UpdateUserResponse>(usr =>
            new PostResult<UpdateUserResponse>(new UpdateUserResponse { User = usr }));
    }

#if TESTINGONLY
    public async Task<ApiGetResult<EndUserWithMemberships, GetUserResponse>> GetUserWithMemberships(
        GetUserRequest request, CancellationToken cancellationToken)
    {
        var user =
            await _endUsersApplication.GetMembershipsAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => user.HandleApplicationResult<EndUserWithMemberships, GetUserResponse>(usr =>
            new GetUserResponse { User = usr });
    }
#endif

    public async Task<ApiResult<EndUser, UpdateUserResponse>> UnassignPlatformRoles(
        UnassignPlatformRolesRequest request, CancellationToken cancellationToken)
    {
        var user =
            await _endUsersApplication.UnassignPlatformRolesAsync(_callerFactory.Create(), request.Id!,
                request.Roles ?? new List<string>(), cancellationToken);

        return () =>
            user.HandleApplicationResult<EndUser, UpdateUserResponse>(usr => new UpdateUserResponse
                { User = usr });
    }
}