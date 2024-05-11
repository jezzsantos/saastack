using Application.Resources.Shared;
using EndUsersApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Invitations;

public class InvitationsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IInvitationsApplication _invitationsApplication;

    public InvitationsApi(ICallerContextFactory callerFactory, IInvitationsApplication invitationsApplication)
    {
        _callerFactory = callerFactory;
        _invitationsApplication = invitationsApplication;
    }

    public async Task<ApiResult<Invitation, VerifyGuestInvitationResponse>> AcceptGuestInvitation(
        VerifyGuestInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation =
            await _invitationsApplication.VerifyGuestInvitationAsync(_callerFactory.Create(), request.Token!,
                cancellationToken);

        return () => invitation.HandleApplicationResult<Invitation, VerifyGuestInvitationResponse>(invite =>
            new VerifyGuestInvitationResponse { Invitation = invite });
    }

    public async Task<ApiPostResult<Invitation, InviteGuestResponse>> InviteGuest(
        InviteGuestRequest request, CancellationToken cancellationToken)
    {
        var invitation =
            await _invitationsApplication.InviteGuestAsync(_callerFactory.Create(), request.Email!,
                cancellationToken);

        return () => invitation.HandleApplicationResult<Invitation, InviteGuestResponse>(invite =>
            new PostResult<InviteGuestResponse>(new InviteGuestResponse { Invitation = invite }));
    }

    public async Task<ApiEmptyResult> ResendGuestInvitation(
        ResendGuestInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation =
            await _invitationsApplication.ResendGuestInvitationAsync(_callerFactory.Create(), request.Token!,
                cancellationToken);

        return () => invitation.HandleApplicationResult();
    }
}