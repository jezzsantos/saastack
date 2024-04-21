using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Microsoft.AspNetCore.Http;
using OrganizationsApplication;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Organizations;

public class OrganizationsApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IFileUploadService _fileUploadService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrganizationsApplication _organizationsApplication;

    public OrganizationsApi(IHttpContextAccessor httpContextAccessor, IFileUploadService fileUploadService,
        ICallerContextFactory contextFactory, IOrganizationsApplication organizationsApplication)
    {
        _httpContextAccessor = httpContextAccessor;
        _fileUploadService = fileUploadService;
        _contextFactory = contextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<ApiPutPatchResult<Organization, GetOrganizationResponse>> AssignRoles(
        AssignRolesToOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await _organizationsApplication.AssignRolesToOrganizationAsync(_contextFactory.Create(),
            request.Id!, request.UserId, request.Roles, cancellationToken);

        return () =>
            organization.HandleApplicationResult<Organization, GetOrganizationResponse>(org =>
                new GetOrganizationResponse { Organization = org });
    }

    public async Task<ApiPutPatchResult<Organization, GetOrganizationResponse>> ChangeAvatar(
        ChangeOrganizationAvatarRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = _httpContextAccessor.HttpContext!.Request;
        var uploaded = httpRequest.GetUploadedFile(_fileUploadService, Validations.Avatar.MaxSizeInBytes,
            Validations.Avatar.AllowableContentTypes);
        if (!uploaded.IsSuccessful)
        {
            return () => uploaded.Error;
        }

        var org =
            await _organizationsApplication.ChangeAvatarAsync(_contextFactory.Create(), request.Id!,
                uploaded.Value, cancellationToken);

        return () =>
            org.HandleApplicationResult<Organization, GetOrganizationResponse>(o =>
                new GetOrganizationResponse { Organization = o });
    }

    public async Task<ApiPutPatchResult<Organization, GetOrganizationResponse>> ChangeOrganization(
        ChangeOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await _organizationsApplication.ChangeDetailsAsync(_contextFactory.Create(),
            request.Id!, request.Name, cancellationToken);

        return () =>
            organization.HandleApplicationResult<Organization, GetOrganizationResponse>(org =>
                new GetOrganizationResponse
                    { Organization = org });
    }

    public async Task<ApiPostResult<Organization, GetOrganizationResponse>> Create(CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.CreateSharedOrganizationAsync(_contextFactory.Create(), request.Name,
                cancellationToken);

        return () => organization.HandleApplicationResult<Organization, GetOrganizationResponse>(org =>
            new PostResult<GetOrganizationResponse>(new GetOrganizationResponse { Organization = org }));
    }

    public async Task<ApiDeleteResult> Delete(DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.DeleteOrganizationAsync(_contextFactory.Create(), request.Id,
                cancellationToken);

        return () => organization.HandleApplicationResult();
    }

    public async Task<ApiResult<Organization, GetOrganizationResponse>> DeleteAvatar(
        DeleteOrganizationAvatarRequest request, CancellationToken cancellationToken)
    {
        var org = await _organizationsApplication.DeleteAvatarAsync(_contextFactory.Create(), request.Id!,
            cancellationToken);

        return () =>
            org.HandleApplicationResult<Organization, GetOrganizationResponse>(o =>
                new GetOrganizationResponse { Organization = o });
    }

    public async Task<ApiGetResult<Organization, GetOrganizationResponse>> Get(GetOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationsApplication.GetOrganizationAsync(_contextFactory.Create(), request.Id!,
                cancellationToken);

        return () =>
            organization.HandleApplicationResult<Organization, GetOrganizationResponse>(org =>
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
            organization.HandleApplicationResult<OrganizationWithSettings, GetOrganizationSettingsResponse>(org =>
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

        return () => organization.HandleApplicationResult<Organization, InviteMemberToOrganizationResponse>(org =>
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

    public async Task<ApiPutPatchResult<Organization, GetOrganizationResponse>> UnassignRoles(
        UnassignRolesFromOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await _organizationsApplication.UnassignRolesFromOrganizationAsync(_contextFactory.Create(),
            request.Id!, request.UserId, request.Roles, cancellationToken);

        return () =>
            organization.HandleApplicationResult<Organization, GetOrganizationResponse>(org =>
                new GetOrganizationResponse { Organization = org });
    }

    public async Task<ApiResult<Organization, UnInviteMemberFromOrganizationResponse>> UnInviteMember(
        UnInviteMemberFromOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await _organizationsApplication.UnInviteMemberFromOrganizationAsync(_contextFactory.Create(),
            request.Id!, request.UserId, cancellationToken);

        return () =>
            organization.HandleApplicationResult<Organization, UnInviteMemberFromOrganizationResponse>(org =>
                new UnInviteMemberFromOrganizationResponse { Organization = org });
    }
}