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

    public async Task<ApiPostResult<EndUser, GetUserResponse>> AssignPlatformRoles(
        AssignPlatformRolesRequest request, CancellationToken cancellationToken)
    {
        var user =
            await _endUsersApplication.AssignPlatformRolesAsync(_callerFactory.Create(), request.Id!,
                request.Roles ?? new List<string>(), cancellationToken);

        return () => user.HandleApplicationResult<EndUser, GetUserResponse>(usr =>
            new PostResult<GetUserResponse>(new GetUserResponse { User = usr }));
    }

    public async Task<ApiResult<EndUser, GetUserResponse>> UnassignPlatformRoles(
        UnassignPlatformRolesRequest request, CancellationToken cancellationToken)
    {
        var user =
            await _endUsersApplication.UnassignPlatformRolesAsync(_callerFactory.Create(), request.Id!,
                request.Roles ?? new List<string>(), cancellationToken);

        return () =>
            user.HandleApplicationResult<EndUser, GetUserResponse>(usr => new GetUserResponse
                { User = usr });
    }
}