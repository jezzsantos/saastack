using Application.Resources.Shared;
using EndUsersApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Memberships;

public class MembershipsApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IEndUsersApplication _endUsersApplication;

    public MembershipsApi(ICallerContextFactory contextFactory, IEndUsersApplication endUsersApplication)
    {
        _contextFactory = contextFactory;
        _endUsersApplication = endUsersApplication;
    }

    public async Task<ApiPutPatchResult<EndUser, GetUserResponse>> ChangeDefaultOrganization(
        ChangeDefaultOrganizationRequest request, CancellationToken cancellationToken)
    {
        var user = await _endUsersApplication.ChangeDefaultMembershipAsync(_contextFactory.Create(),
            request.OrganizationId, cancellationToken);

        return () => user.HandleApplicationResult<EndUser, GetUserResponse>(x => new GetUserResponse { User = x });
    }

    public async Task<ApiSearchResult<Membership, ListMembershipsForCallerResponse>> ListMembershipsForCaller(
        ListMembershipsForCallerRequest request, CancellationToken cancellationToken)
    {
        var memberships = await _endUsersApplication.ListMembershipsForCallerAsync(_contextFactory.Create(),
            request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () => memberships.HandleApplicationResult(ms => new ListMembershipsForCallerResponse
            { Memberships = ms.Results, Metadata = ms.Metadata });
    }
}