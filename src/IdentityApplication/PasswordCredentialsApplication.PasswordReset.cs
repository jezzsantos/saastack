using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

partial class PasswordCredentialsApplication
{
    public async Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByUsernameAsync(emailAddress, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            var warned =
                await _userNotificationsService.NotifyPasswordResetUnknownUserCourtesyAsync(caller, emailAddress,
                    UserNotificationConstants.EmailTags.PasswordResetUnknownUser, cancellationToken);
            if (warned.IsFailure)
            {
                return warned.Error;
            }

            return Result.Ok;
        }

        var credentials = retrieved.Value.Value;
        var initiated = credentials.InitiatePasswordReset();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var registration = credentials.Registration.Value;
        var notified = await _userNotificationsService.NotifyPasswordResetInitiatedAsync(caller, registration.Name,
            emailAddress, credentials.Password.ResetToken, UserNotificationConstants.EmailTags.PasswordResetInitiated,
            cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password reset initiated for {Id}", credentials.UserId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserPasswordForgotten,
            new Dictionary<string, object>
            {
                { nameof(PasswordCredential.Id), credentials.UserId }
            });

        return Result.Ok;
    }

    public async Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var initiated = credentials.InitiatePasswordReset();
        if (initiated.IsFailure)
        {
            return initiated.Error;
        }

        var registration = credentials.Registration.Value;
        var notified = await _userNotificationsService.NotifyPasswordResetInitiatedAsync(caller, registration.Name,
            registration.EmailAddress, credentials.Password.ResetToken,
            UserNotificationConstants.EmailTags.PasswordResetResend, cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password reset re-initiated for {Id}", credentials.UserId);

        return Result.Ok;
    }

    public async Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var verified = credentials.VerifyPasswordReset(token);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Password reset verified for {Id}", credentials.UserId);

        return Result.Ok;
    }

    public async Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindCredentialsByPasswordResetTokenAsync(token, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var credentials = retrieved.Value.Value;
        var reset = credentials.CompletePasswordReset(token, password);
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var saved = await _repository.SaveAsync(credentials, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        credentials = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Password was reset for {Id}", credentials.UserId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserPasswordReset,
            new Dictionary<string, object>
            {
                { nameof(credentials.Id), credentials.UserId }
            });

        return Result.Ok;
    }
}