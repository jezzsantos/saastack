using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class LoginMonitorSpec
{
    [Fact]
    public void WhenConstructed_ThenPropertiesAssigned()
    {
        var history = LoginMonitor.Create(1, TimeSpan.FromHours(1)).Value;

        history.MaxFailedPasswordAttempts.Should().Be(1);
        history.FailedPasswordAttempts.Should().Be(0);
        history.CooldownPeriod.Should().Be(TimeSpan.FromHours(1));
        history.FailedPasswordAttempts.Should().Be(0);
        history.LastAttemptUtc.Should().BeNone();
        history.IsLocked.Should().BeFalse();
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeTrue();
    }

    [Fact]
    public void WhenAttemptedSuccessfulLoginOnce_TheReturnsHistory()
    {
        var datum = DateTime.UtcNow;
        var history = LoginMonitor.Create(1, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedSuccessfulLogin(datum);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginOnceAndFailedAttemptsIsLessThanMax_TheReturnsHistory()
    {
        var datum = DateTime.UtcNow;
        var history = LoginMonitor.Create(2, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(1);
        ((object)history.LastAttemptUtc).Should().Be(datum);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginsTwiceAndFailedAttemptsIsLessThanMax_TheReturnsHistory()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var history = LoginMonitor.Create(3, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(2);
        ((object)history.LastAttemptUtc).Should().Be(datum2);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginAndFailedAttemptsIsLessThanMaxAndThenLoginSuccessful_TheReturnsHistory()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var history = LoginMonitor.Create(3, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedSuccessfulLogin(datum3);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum3);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginAndFailedAttemptsIsMax_TheReturnsHistory()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var history = LoginMonitor.Create(2, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);

        history.IsLocked.Should().BeTrue();
        history.FailedPasswordAttempts.Should().Be(2);
        ((object)history.LastAttemptUtc).Should().Be(datum2);
        history.ToggledLocked.Should().BeTrue();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginAndFailedAttemptsIsOneMoreThanMax_TheReturnsHistory()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var history = LoginMonitor.Create(2, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);

        history.IsLocked.Should().BeTrue();
        history.FailedPasswordAttempts.Should().Be(3);
        ((object)history.LastAttemptUtc).Should().Be(datum3);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginAndFailedAttemptsIsManyMoreThanMax_TheReturnsHistory()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var datum4 = datum3.AddSeconds(1);
        var history = LoginMonitor.Create(2, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedFailedLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);
        history = history.AttemptedFailedLogin(datum4);

        history.IsLocked.Should().BeTrue();
        history.FailedPasswordAttempts.Should().Be(4);
        ((object)history.LastAttemptUtc).Should().Be(datum4);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void
        WhenAttemptedSuccessfulLoginAndFailedAttemptsIsMoreThanMaxWithinCooldownPeriod_ThenStillReturnsTrue()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var datum4 = datum3.AddSeconds(1);
        var datum5 = datum4.AddSeconds(1);
        var history = LoginMonitor.Create(2, TimeSpan.FromHours(1)).Value;
        history = history.AttemptedSuccessfulLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);
        history = history.AttemptedFailedLogin(datum4);
        history = history.AttemptedSuccessfulLogin(datum5);

        history.IsLocked.Should().BeTrue();
        history.FailedPasswordAttempts.Should().Be(3);
        ((object)history.LastAttemptUtc).Should().Be(datum5);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedSuccessfulLoginAndFailedAttemptsIsMoreThanMaxAfterCooldownPeriod_ThenReturnsFalse()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var datum4 = datum3.AddSeconds(1);
        var cooldownPeriod = TimeSpan.FromSeconds(1);
        var history = LoginMonitor.Create(2, cooldownPeriod).Value;
        history = history.AttemptedSuccessfulLogin(datum1);
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);
        history = history.AttemptedFailedLogin(datum4);

        history.IsLocked.Should().BeTrue();

        var remainingSleep = datum4.Add(cooldownPeriod).AddSeconds(1).Subtract(DateTime.UtcNow);
        Thread.Sleep(remainingSleep);

        history.IsLocked.Should().BeFalse();

        var datum5 = datum4.AddSeconds(1);
        history = history.AttemptedSuccessfulLogin(datum5);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum5);
        history.ToggledLocked.Should().BeTrue();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenAttemptedFailedLoginAndFailedAttemptsIsMoreThanMaxAndAfterCooldownPeriod_ThenReturnsFalse()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var datum4 = datum3.AddSeconds(1);
        var cooldownPeriod = TimeSpan.FromSeconds(1);
        var history = LoginMonitor.Create(2, cooldownPeriod).Value;
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);
        history = history.AttemptedFailedLogin(datum4);

        history.IsLocked.Should().BeTrue();

        var remainingSleep = datum4.Add(cooldownPeriod).AddSeconds(1).Subtract(DateTime.UtcNow);
        Thread.Sleep(remainingSleep);

        history.IsLocked.Should().BeFalse();

        var datum5 = datum4.AddSeconds(1);
        history = history.AttemptedFailedLogin(datum5);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(1);
        ((object)history.LastAttemptUtc).Should().Be(datum5);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenUnlockAndUnlocked_ThenReturnsUnlocked()
    {
        var datum = DateTime.UtcNow;
        var cooldownPeriod = TimeSpan.FromSeconds(1);
        var history = LoginMonitor.Create(2, cooldownPeriod).Value;

        history = history.Unlock(datum);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum);
        history.ToggledLocked.Should().BeFalse();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenUnlockAndLockedAndInCooldownPeriod_ThenReturnsUnlocked()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var datum4 = datum3.AddSeconds(1);
        var cooldownPeriod = TimeSpan.FromSeconds(1);
        var history = LoginMonitor.Create(2, cooldownPeriod).Value;
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);

        history.IsLocked.Should().BeTrue();

        history = history.Unlock(datum4);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum4);
        history.ToggledLocked.Should().BeTrue();
        history.IsReset.Should().BeFalse();
    }

    [Fact]
    public void WhenUnlockAndLockedAndAfterCooldownPeriod_ThenReturnsUnlocked()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var datum3 = datum2.AddSeconds(1);
        var cooldownPeriod = TimeSpan.FromSeconds(1);
        var history = LoginMonitor.Create(2, cooldownPeriod).Value;
        history = history.AttemptedFailedLogin(datum2);
        history = history.AttemptedFailedLogin(datum3);

        history.IsLocked.Should().BeTrue();

        var remainingSleep = datum3.Add(cooldownPeriod).AddSeconds(1).Subtract(DateTime.UtcNow);
        Thread.Sleep(remainingSleep);

        history.IsLocked.Should().BeFalse();

        history = history.Unlock(datum3);

        history.IsLocked.Should().BeFalse();
        history.FailedPasswordAttempts.Should().Be(0);
        ((object)history.LastAttemptUtc).Should().Be(datum3);
        history.ToggledLocked.Should().BeTrue();
        history.IsReset.Should().BeFalse();
    }
}