using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class EventStreamSpec
{
    private readonly EventStream _stream;

    public EventStreamSpec()
    {
        _stream = EventStream.Create();
    }

    [Fact]
    public void WhenCreateWithNoVersions_ThenVersionsSet()
    {
        var result = EventStream.Create();

        result.FirstEventVersion.Should().Be(EventStream.NoVersion);
        result.LastEventVersion.Should().Be(EventStream.NoVersion);
    }

    [Fact]
    public void WhenCreateWithZeroFirstVersion_ThenReturnsStream()
    {
        var result = EventStream.Create(0, 1);

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(0);
        result.Value.LastEventVersion.Should().Be(1);
    }

    [Fact]
    public void WhenCreateWithZeroLastVersion_ThenReturnsStream()
    {
        var result = EventStream.Create(1, 0);

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(1);
        result.Value.LastEventVersion.Should().Be(0);
    }

    [Fact]
    public void WhenCreateWithNegativeFirstVersion_ThenReturnsStream()
    {
        var result = EventStream.Create(-1, 1);

        result.Should().BeError(ErrorCode.Validation, Resources.EventStream_ZeroFirstVersion);
    }

    [Fact]
    public void WhenCreateWithNegativeLastVersion_ThenReturnsStream()
    {
        var result = EventStream.Create(1, -1);

        result.Should().BeError(ErrorCode.Validation, Resources.EventStream_ZeroLastVersion);
    }

    [Fact]
    public void WhenNext_ThenIncrementsLastVersion()
    {
        var result = _stream.Next();

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(EventStream.NoVersion);
        result.Value.LastEventVersion.Should().Be(EventStream.NoVersion + 1);
    }

    [Fact]
    public void WhenNextAgain_ThenIncrementsLastVersion()
    {
        var result = _stream.Next();
        result = result.Value.Next();

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(EventStream.NoVersion);
        result.Value.LastEventVersion.Should().Be(EventStream.NoVersion + 2);
    }

    [Fact]
    public void WhenUpdateChangeWithNoVersion_ThenError()
    {
        var result = _stream.UpdateChange(EventStream.NoVersion);

        result.IsSuccessful.Should().BeFalse();
        result.Should().BeError(ErrorCode.Validation,
            Resources.EventStream_OutOfOrderChange.Format(EventStream.NoVersion, EventStream.FirstVersion));
    }

    [Fact]
    public void WhenUpdateChangeAndVersionIsOneAndFirstTime_ThenSucceeds()
    {
        var result = _stream.UpdateChange(1);

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(1);
        result.Value.LastEventVersion.Should().Be(1);
    }

    [Fact]
    public void WhenUpdateChangeAndVersionIsNotOneAndFirstTime_ThenSucceeds()
    {
        var result = _stream.UpdateChange(10);

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(10);
        result.Value.LastEventVersion.Should().Be(10);
    }

    [Fact]
    public void WhenUpdateChangeAndVersionIsNotOneAndNextTime_ThenSucceeds()
    {
        var result = _stream.UpdateChange(10);
        result = result.Value.UpdateChange(11);

        result.Should().BeSuccess();
        result.Value.FirstEventVersion.Should().Be(10);
        result.Value.LastEventVersion.Should().Be(11);
    }

    [Fact]
    public void WhenUpdateChangeAndVersionIsNotOneAndNextTimeOutOfOrder_ThenError()
    {
        var result = _stream.UpdateChange(10);
        result = result.Value.UpdateChange(11);

        result = result.Value.UpdateChange(20);

        result.IsSuccessful.Should().BeFalse();
        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EventStream_OutOfOrderChange.Format(20, 12));
    }
}