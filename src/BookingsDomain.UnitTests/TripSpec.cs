using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace BookingsDomain.UnitTests;

[Trait("Category", "Unit")]
public class TripSpec
{
    private readonly Trip _trip;

    public TripSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new FixedIdentifierFactory("anid");

        _trip = Trip.Create(recorder.Object, idFactory, _ => Result.Ok).Value;
        _trip.RaiseChangeEvent(TripAdded.Create("arootid".ToId(), "anorganizationid".ToId()));
    }

    [Fact]
    public void WhenBegin_ThenReturnsSuccess()
    {
        var result = _trip.Begin(Location.Create("alocation").Value);

        result.Should().BeSuccess();
        _trip.BeganAt.Value.Should().BeNear(DateTime.UtcNow);
        _trip.From.Value.Name.Should().Be("alocation");
    }

    [Fact]
    public void WhenBeginAndAlreadyStarted_ThenReturnsError()
    {
        _trip.Begin(Location.Create("alocation").Value);

        var result = _trip.Begin(Location.Create("alocation").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.TripEntity_AlreadyBegan);
    }

    [Fact]
    public void WhenEndButNotStarted_ThenReturnsError()
    {
        var result = _trip.End(Location.Create("alocation").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.TripEntity_NotBegun);
    }

    [Fact]
    public void WhenEndAndAlreadyEnded_ThenReturnsError()
    {
        _trip.Begin(Location.Create("alocation").Value);
        _trip.End(Location.Create("alocation").Value);

        var result = _trip.End(Location.Create("alocation").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.TripEntity_AlreadyEnded);
    }

    [Fact]
    public void WhenEndAndAlreadyStarted_ThenReturnsSuccess()
    {
        _trip.Begin(Location.Create("astart").Value);

        var result = _trip.End(Location.Create("anend").Value);

        result.Should().BeSuccess();
        _trip.BeganAt.Value.Should().BeNear(DateTime.UtcNow);
        _trip.From.Value.Name.Should().Be("astart");
        _trip.EndedAt.Value.Should().BeNear(DateTime.UtcNow);
        _trip.To.Value.Name.Should().Be("anend");
    }

    [Fact]
    public void WhenEnsureInvariantsAndNotStarted_ThenReturnsSuccess()
    {
        var result = _trip.EnsureInvariants();

        result.Should().BeSuccess();
    }

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndBeganWithNoLocation_ThenReturnsError()
    {
        _trip.Begin(Location.Create("alocation").Value);
        _trip.TestingOnly_Assign(Optional<Location>.None, Location.Create("alocation").Value);

        var result = _trip.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.TripEntity_NoStartingLocation);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndEndedWithNoLocation_ThenReturnsError()
    {
        _trip.Begin(Location.Create("alocation").Value);
        _trip.End(Location.Create("alocation").Value);
        _trip.TestingOnly_Assign(Location.Create("alocation").Value, Optional<Location>.None);

        var result = _trip.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.TripEntity_NoEndingLocation);
    }
#endif
}