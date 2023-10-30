using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class CausedBySpec
{
    [Fact]
    public void WhenCreateAndReferenceNullForReservation_ThenReturnsError()
    {
        var result = CausedBy.Create(UnavailabilityCausedBy.Reservation, null);

        result.Should().BeError(ErrorCode.Validation, Resources.CausedBy_ReservationWithoutReference);
    }

    [Fact]
    public void WhenCreateAndReferenceNullForOther_ThenReturnsCausedBy()
    {
        var result = CausedBy.Create(UnavailabilityCausedBy.Other, null);

        result.Should().BeSuccess();
        result.Value.Reason.Should().Be(UnavailabilityCausedBy.Other);
        result.Value.Reference.Should().BeNull();
    }

    [Fact]
    public void WhenCreateAndReferenceInvalid_ThenReturnsError()
    {
        var result = CausedBy.Create(UnavailabilityCausedBy.Other, "^aninvalidreference^");

        result.Should().BeError(ErrorCode.Validation, Resources.CausedBy_InvalidReference);
    }

    [Fact]
    public void WhenCreateAndReferenceValidForOther_ThenReturnsCausedBy()
    {
        var result = CausedBy.Create(UnavailabilityCausedBy.Other, "areference");

        result.Should().BeSuccess();
        result.Value.Reason.Should().Be(UnavailabilityCausedBy.Other);
        result.Value.Reference.Should().Be("areference");
    }
}