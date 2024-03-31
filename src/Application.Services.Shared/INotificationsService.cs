using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a notifications service for alerting users
/// </summary>
public interface INotificationsService
{
    /// <summary>
    ///     Notifies a user, via email, that they have been invited to register with the platform
    /// </summary>
    Task<Result<Error>> NotifyGuestInvitationToPlatformAsync(ICallerContext caller, string token,
        string inviteeEmailAddress, string inviteeName, string inviterName, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, to confirm their account registration
    /// </summary>
    Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller, string emailAddress,
        string name, string token, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, to warn them that an attempt to re-register an account by another party has occurred
    /// </summary>
    Task<Result<Error>> NotifyReRegistrationCourtesyAsync(ICallerContext caller, string userId, string emailAddress,
        string name, string? timezone, string? countryCode, CancellationToken cancellationToken);
}