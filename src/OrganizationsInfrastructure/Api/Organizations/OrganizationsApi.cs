using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.Api.Organizations;

public class OrganizationsApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IOrganizationsApplication _organizationsApplication;

    public OrganizationsApi(ICallerContextFactory contextFactory, IOrganizationsApplication organizationsApplication)
    {
        _contextFactory = contextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<ApiPostResult<Organization, GetOrganizationResponse>> Create(CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.CreateSharedOrganizationAsync(_contextFactory.Create(), request.Name,
                cancellationToken);

        return () => organization.HandleApplicationResult<GetOrganizationResponse, Organization>(org =>
            new PostResult<GetOrganizationResponse>(new GetOrganizationResponse { Organization = org }));
    }

    public async Task<ApiGetResult<Organization, GetOrganizationResponse>> Get(GetOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.GetOrganizationAsync(_contextFactory.Create(), request.Id!,
                cancellationToken);

        return () =>
            organization.HandleApplicationResult<GetOrganizationResponse, Organization>(org =>
                new GetOrganizationResponse { Organization = org });
    }

#if TESTINGONLY
    public async Task<ApiGetResult<OrganizationWithSettings, GetOrganizationSettingsResponse>> GetSettings(
        GetOrganizationSettingsRequest request, CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.GetOrganizationSettingsAsync(_contextFactory.Create(), request.Id!,
                cancellationToken);

        return () =>
            organization.HandleApplicationResult<GetOrganizationSettingsResponse, OrganizationWithSettings>(org =>
                new GetOrganizationSettingsResponse
                {
                    Organization = org,
                    Settings = org.Settings
                });
    }
#endif

    public async Task<ApiPostResult<Organization, InviteMemberToOrganizationResponse>> InviteMember(
        InviteMemberToOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.InviteMemberToOrganizationAsync(_contextFactory.Create(), request.Id!,
                request.UserId, request.Email,
                cancellationToken);

        return () => organization.HandleApplicationResult<InviteMemberToOrganizationResponse, Organization>(org =>
            new PostResult<InviteMemberToOrganizationResponse>(new InviteMemberToOrganizationResponse
                { Organization = org }));
    }

    public async Task<ApiSearchResult<OrganizationMember, ListMembersForOrganizationResponse>> ListMembers(
        ListMembersForOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var members =
            await _organizationsApplication.ListMembersForOrganizationAsync(_contextFactory.Create(), request.Id,
                request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            members.HandleApplicationResult(m =>
                new ListMembersForOrganizationResponse { Members = m.Results, Metadata = m.Metadata });
    }
}