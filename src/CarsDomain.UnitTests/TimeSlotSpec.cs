using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class TimeSlotSpec
{
    [Fact]
    public void WhenCreateAndMinFrom_ThenReturnsError()
    {
        var result = TimeSlot.Create(DateTime.MinValue, DateTime.UtcNow);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndMinTo_ThenReturnsError()
    {
        var result = TimeSlot.Create(DateTime.UtcNow, DateTime.MinValue);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndToNotAfterFrom_ThenReturnsError()
    {
        var datum = DateTime.UtcNow;
        var result = TimeSlot.Create(datum, datum);

        result.Should().BeError(ErrorCode.Validation, Resources.TimeSlot_FromDateBeforeToDate);
    }

    [Fact]
    public void WhenCreateAndToAfterFrom_ThenReturnsSlot()
    {
        var datum = DateTime.UtcNow;
        var result = TimeSlot.Create(datum, datum.AddSeconds(1));

        result.Should().BeSuccess();
        result.Value.From.Should().Be(datum);
        result.Value.To.Should().Be(datum.AddSeconds(1));
    }
}