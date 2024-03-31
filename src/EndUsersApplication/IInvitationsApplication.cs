using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace EndUsersApplication;

public interface IInvitationsApplication
{
    Task<Result<Invitation, Error>> InviteGuestAsync(ICallerContext context, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Error>> ResendGuestInvitationAsync(ICallerContext context, string token,
        CancellationToken cancellationToken);

    Task<Result<Invitation, Error>> VerifyGuestInvitationAsync(ICallerContext context, string token,
        CancellationToken cancellationToken);
}