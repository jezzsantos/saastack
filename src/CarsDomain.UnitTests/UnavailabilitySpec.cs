using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class UnavailabilitySpec
{
    private readonly DateTime _end;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly DateTime _start;
    private readonly Unavailability _unavailability;

    public UnavailabilitySpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _start = DateTime.UtcNow;
        _end = _start.AddHours(1);
        _unavailability = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok).Value;
    }

    [Fact]
    public void WhenOverlapsAndNotAssigned_ThenReturnsError()
    {
        var result = _unavailability.Overlaps(TimeSlot.Create(_end, _end.AddHours(1)).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UnavailabilityEntity_NotAssigned);
    }

#if TESTINGONLY
    [Fact]
    public void WhenOverlapsAndNotOverlapping_ThenReturnsFalse()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        var slot = TimeSlot.Create(_end, _end.AddHours(1)).Value;

        var result = _unavailability.Overlaps(slot).Value;

        result.Should().BeFalse();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenOverlapsAndOverlapping_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        var slot = TimeSlot.Create(_start.SubtractHours(1), _end.AddHours(1)).Value;

        var result = _unavailability.Overlaps(slot).Value;

        result.Should().BeTrue();
    }
#endif

    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedByInEither_ThenReturnsFalse()
    {
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedInSource_ThenReturnsTrue()
    {
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedInOther_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHaveSameCausesAndNoReferences_ThenReturnsFalse()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeFalse();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHaveSameCausesAndDifferentReferences_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference2").Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndNullReference_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Maintenance, null).Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndSameReference_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference").Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value,
            CausedBy.Create(UnavailabilityCausedBy.Maintenance, "areference").Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndDifferentReference_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value,
            CausedBy.Create(UnavailabilityCausedBy.Maintenance, "areference2").Value);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }
#endif

    [Fact]
    public void WhenEnsureInvariantsAndNoDetails_ThenReturnsError()
    {
        var result = _unavailability.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UnavailabilityEntity_NotAssigned);
    }

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndAssigned_ThenReturnsTrue()
    {
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);

        var result = _unavailability.EnsureInvariants();

        result.Should().BeSuccess();
    }
#endif
}