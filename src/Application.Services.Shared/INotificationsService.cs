using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a notifications service for alerting users
/// </summary>
public interface INotificationsService
{
    /// <summary>
    ///     Notifies a user, via email, to confirm their account registration
    /// </summary>
    Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller, string emailAddress,
        string name, string token, CancellationToken cancellationToken);
}