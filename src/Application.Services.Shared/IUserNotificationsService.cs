using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a notifications service for alerting users
/// </summary>
public interface IUserNotificationsService
{
    /// <summary>
    ///     Notifies a user, via email, that they have been invited to register with the platform
    /// </summary>
    Task<Result<Error>> NotifyGuestInvitationToPlatformAsync(ICallerContext caller, string token,
        string inviteeEmailAddress, string inviteeName, string inviterName, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, with a two-factor code to complete MFA authentication
    /// </summary>
    Task<Result<Error>> NotifyPasswordMfaOobEmailAsync(ICallerContext caller, string emailAddress, string code,
        IReadOnlyList<string>? tags, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via SMS text message, with a two-factor code to complete MFA authentication
    /// </summary>
    Task<Result<Error>> NotifyPasswordMfaOobSmsAsync(ICallerContext caller, string phoneNumber, string code,
        IReadOnlyList<string>? tags, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, to confirm their account registration
    /// </summary>
    Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller, string emailAddress,
        string name, string token, IReadOnlyList<string>? tags, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, to warn them that an attempt to re-register an account by another party has occurred
    /// </summary>
    Task<Result<Error>> NotifyPasswordRegistrationRepeatCourtesyAsync(ICallerContext caller, string userId,
        string emailAddress, string name, string? timezone, string? countryCode, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies a user, via email, that their password reset has been initiated
    /// </summary>
    Task<Result<Error>> NotifyPasswordResetInitiatedAsync(ICallerContext caller, string name, string emailAddress,
        string token, IReadOnlyList<string>? tags, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies an unknown user, via email, that their email has been used to initiate a password reset
    /// </summary>
    Task<Result<Error>> NotifyPasswordResetUnknownUserCourtesyAsync(ICallerContext caller, string emailAddress,
        IReadOnlyList<string>? tags, CancellationToken cancellationToken);
}