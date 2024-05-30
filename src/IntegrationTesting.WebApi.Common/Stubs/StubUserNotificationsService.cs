using Application.Interfaces;
using Application.Services.Shared;
using Common;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IUserNotificationsService" />
/// </summary>
public class StubUserNotificationsService : IUserNotificationsService
{
    public string? LastEmailChangeConfirmationToken { get; private set; }

    public string? LastEmailChangeRecipient { get; private set; }

    public string? LastGuestInvitationEmailRecipient { get; private set; }

    public string? LastGuestInvitationToken { get; private set; }

    public string? LastPasswordResetCourtesyEmailRecipient { get; private set; }

    public string? LastPasswordResetEmailRecipient { get; private set; }

    public string? LastPasswordResetToken { get; private set; }

    public string? LastRegistrationConfirmationEmailRecipient { get; private set; }

    public string? LastRegistrationConfirmationToken { get; private set; }

    public string? LastReRegistrationCourtesyEmailRecipient { get; private set; }

    public Task<Result<Error>> NotifyGuestInvitationToPlatformAsync(ICallerContext caller, string token,
        string inviteeEmailAddress,
        string inviteeName, string inviterName, CancellationToken cancellationToken)
    {
        LastGuestInvitationEmailRecipient = inviteeEmailAddress;
        LastGuestInvitationToken = token;
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller, string emailAddress,
        string name, string token, CancellationToken cancellationToken)
    {
        LastRegistrationConfirmationEmailRecipient = emailAddress;
        LastRegistrationConfirmationToken = token;
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> NotifyPasswordResetInitiatedAsync(ICallerContext caller, string name,
        string emailAddress, string token,
        CancellationToken cancellationToken)
    {
        LastPasswordResetEmailRecipient = emailAddress;
        LastPasswordResetToken = token;
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> NotifyPasswordResetUnknownUserCourtesyAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        LastPasswordResetCourtesyEmailRecipient = emailAddress;
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> NotifyPasswordRegistrationRepeatCourtesyAsync(ICallerContext caller, string userId,
        string emailAddress, string name,
        string? timezone, string? countryCode, CancellationToken cancellationToken)
    {
        LastReRegistrationCourtesyEmailRecipient = emailAddress;
        return Task.FromResult(Result.Ok);
    }

    public void Reset()
    {
        LastRegistrationConfirmationEmailRecipient = null;
        LastEmailChangeRecipient = null;
        LastPasswordResetEmailRecipient = null;
        LastPasswordResetCourtesyEmailRecipient = null;
        LastReRegistrationCourtesyEmailRecipient = null;
        LastRegistrationConfirmationToken = null;
        LastEmailChangeConfirmationToken = null;
        LastGuestInvitationEmailRecipient = null;
        LastGuestInvitationToken = null;
        LastPasswordResetToken = null;
    }
}