using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace IdentityDomain;

public sealed class LoginMonitor : ValueObjectBase<LoginMonitor>
{
    private bool _hasCooldownPeriodJustExpired;

    public static Result<LoginMonitor, Error> Create(int maxFailedPasswordAttempts, TimeSpan cooldownPeriod)
    {
        if (maxFailedPasswordAttempts.IsInvalidParameter(Validations.Credentials.Login.MaxFailedPasswordAttempts,
                nameof(maxFailedPasswordAttempts), Resources.LoginMonitor_InvalidMaxFailedLogins, out var error1))
        {
            return error1;
        }

        if (cooldownPeriod.IsInvalidParameter(Validations.Credentials.Login.CooldownPeriod, nameof(cooldownPeriod),
                Resources.LoginMonitor_InvalidCooldownPeriod, out var error2))
        {
            return error2;
        }

        return new LoginMonitor(maxFailedPasswordAttempts, cooldownPeriod, 0, Optional<DateTime>.None, false);
    }

    private LoginMonitor(int maxFailedPasswordAttempts, TimeSpan cooldownPeriod, int failedPasswordAttempts,
        Optional<DateTime> lastAttemptUtc, bool hasToggleLocked)
    {
        MaxFailedPasswordAttempts = maxFailedPasswordAttempts;
        CooldownPeriod = cooldownPeriod;
        FailedPasswordAttempts = failedPasswordAttempts;
        LastAttemptUtc = lastAttemptUtc;
        ToggledLocked = hasToggleLocked;
        _hasCooldownPeriodJustExpired = false;
    }

    public TimeSpan CooldownPeriod { get; }

    public int FailedPasswordAttempts { get; }

    public bool HasJustLocked => ToggledLocked && IsLocked;

    public bool HasJustUnlocked => ToggledLocked && !IsLocked;

    private bool HasJustUnlockedInternal => !IsLocked && _hasCooldownPeriodJustExpired;

    public bool IsLocked
    {
        get
        {
            var hasFailedTooManyTimes = FailedPasswordAttempts >= MaxFailedPasswordAttempts;
            if (!hasFailedTooManyTimes)
            {
                return false;
            }

            var isInCooldown = IsAttemptStillWithinCooldownPeriod();
            if (!isInCooldown)
            {
                _hasCooldownPeriodJustExpired = true;
            }

            return isInCooldown;
        }
    }

    public bool IsReset => !LastAttemptUtc.HasValue && FailedPasswordAttempts == 0;

    public Optional<DateTime> LastAttemptUtc { get; }

    internal int MaxFailedPasswordAttempts { get; }

    internal bool ToggledLocked { get; }

    public static ValueObjectFactory<LoginMonitor> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new LoginMonitor(parts[0].ToIntOrDefault(0),
                parts[1].ToTimeSpanOrDefault(TimeSpan.Zero),
                parts[2].ToIntOrDefault(0),
                parts[3].FromValueOrNone(val => val.FromIso8601()),
                parts[4].ToBool());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[]
        {
            MaxFailedPasswordAttempts, CooldownPeriod, FailedPasswordAttempts,
            LastAttemptUtc.ToValueOrNull(val => val.ToIso8601()), ToggledLocked
        };
    }

    public LoginMonitor AttemptedFailedLogin(DateTime attemptedUtc)
    {
        var incrementedFailedAttempts = HasJustUnlockedInternal
            ? 1
            : FailedPasswordAttempts + 1;
        var hasReachedMaxFailed = incrementedFailedAttempts == MaxFailedPasswordAttempts;
        var isAboutToBecomeLocked = hasReachedMaxFailed && !IsLocked && !ToggledLocked;

        return Attempt(incrementedFailedAttempts, attemptedUtc, isAboutToBecomeLocked);
    }

    public LoginMonitor AttemptedSuccessfulLogin(DateTime attemptedUtc)
    {
        if (IsLocked)
        {
            return Attempt(attemptedUtc, false);
        }

        return Attempt(0, attemptedUtc, HasJustUnlockedInternal);
    }

#if TESTINGONLY

    public LoginMonitor TestingOnly_ResetCooldownPeriod()
    {
        return Attempt(DateTime.MinValue, false);
    }
#endif

    public LoginMonitor Unlock(DateTime attemptedUtc)
    {
        if (IsLocked || HasJustUnlockedInternal)
        {
            return Attempt(0, attemptedUtc, true);
        }

        return Attempt(attemptedUtc, false);
    }

    private bool IsAttemptStillWithinCooldownPeriod()
    {
        if (!LastAttemptUtc.HasValue)
        {
            return false;
        }

        var endOfCooldownPeriod = LastAttemptUtc.Value
            .Add(CooldownPeriod);

        return DateTime.UtcNow.IsBefore(endOfCooldownPeriod);
    }

    private LoginMonitor Attempt(DateTime lastAttemptUtc, bool hasToggleLocked)
    {
        return Attempt(FailedPasswordAttempts, lastAttemptUtc, hasToggleLocked);
    }

    private LoginMonitor Attempt(int failedPasswordAttempts, DateTime lastAttemptUtc, bool hasToggleLocked)
    {
        return new LoginMonitor(MaxFailedPasswordAttempts, CooldownPeriod, failedPasswordAttempts,
            lastAttemptUtc, hasToggleLocked);
    }
}