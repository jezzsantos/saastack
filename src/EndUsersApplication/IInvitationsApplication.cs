using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace EndUsersApplication;

public partial interface IInvitationsApplication
{
    Task<Result<Invitation, Error>> InviteGuestAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Error>> ResendGuestInvitationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

    Task<Result<Invitation, Error>> VerifyGuestInvitationAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);
}