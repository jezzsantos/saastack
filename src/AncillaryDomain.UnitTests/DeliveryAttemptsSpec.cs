using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class DeliveryAttemptsSpec
{
    [Fact]
    public void WhenEmpty_ThenHasNoAttempts()
    {
        var result = DeliveryAttempts.Empty;

        result.Attempts.Should().BeEmpty();
        result.HasBeenAttempted.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateWithPrevious_ThenHasAttempts()
    {
        var datum = DateTime.UtcNow;

        var result = DeliveryAttempts.Create(new List<DateTime> { datum }).Value;

        result.Attempts.Should().ContainInOrder(datum);
        result.HasBeenAttempted.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithPreviousButOutOfOrder_ThenReturnsError()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);

        var result = DeliveryAttempts.Create(new List<DateTime> { datum2, datum1 });

        result.Should().BeError(ErrorCode.Validation, Resources.DeliveryAttempts_PreviousAttemptsNotInOrder);
    }

    [Fact]
    public void WhenCreateWithPreviousAndInOrder_ThenReturnsAttempts()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);

        var result = DeliveryAttempts.Create(new List<DateTime> { datum1, datum2 });

        result.Should().BeSuccess();
        result.Value.Attempts.Should().ContainInOrder(datum1, datum2);
        result.Value.HasBeenAttempted.Should().BeTrue();
    }

    [Fact]
    public void WhenAttemptAndBeforeLast_ThenReturnsError()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var attempts = DeliveryAttempts.Create(new List<DateTime> { datum1, datum2 }).Value;

        var result = attempts.Attempt(datum1);

        result.Should().BeError(ErrorCode.Validation, Resources.DeliveryAttempts_LatestAttemptNotAfterLastAttempt);
    }

    [Fact]
    public void WhenAttemptAndAfterLast_ThenReturnsAttempts()
    {
        var datum1 = DateTime.UtcNow;
        var datum2 = datum1.AddSeconds(1);
        var attempts = DeliveryAttempts.Create(new List<DateTime> { datum1, datum2 }).Value;
        var datum3 = datum2.AddSeconds(1);

        var result = attempts.Attempt(datum3);

        result.Should().BeSuccess();
        result.Value.Attempts.Should().ContainInOrder(datum1, datum2, datum3);
        result.Value.HasBeenAttempted.Should().BeTrue();
    }
}