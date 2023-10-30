using Common.Recording;
using FluentAssertions;
using Moq;
using Xunit;

namespace Common.UnitTests.Recording;

[Trait("Category", "Unit")]
public class RecordingExtensionsSpec
{
    [Fact]
    public void WhenMeasureWithAnAction_ThenMeasures()
    {
        var recorder = new Mock<IRecorder>();
        var call = new Mock<ICallContext>();
        var wasCalled = false;
        var action = (Dictionary<string, object> additional) =>
        {
            additional.Add("akey", "avalue");
            wasCalled = true;
            return 1;
        };

        var result = recorder.Object.MeasureWith(call.Object, "aneventname", action);

        result.Should().Be(1);
        wasCalled.Should().BeTrue();
        recorder.Verify(rec => rec.Measure(call.Object, "aneventname", It.Is<Dictionary<string, object>>(dic =>
            dic.Count == 1
            && (string)dic["akey"] == "avalue"
        )));
    }

    [Fact]
    public void WhenMeasureWithDurationWithAnAction_ThenMeasuresAndAddsDuration()
    {
        var recorder = new Mock<IRecorder>();
        var call = new Mock<ICallContext>();
        var wasCalled = false;
        var action = (Dictionary<string, object> additional) =>
        {
            additional.Add("akey", "avalue");
            wasCalled = true;
            return 1;
        };

        var result = recorder.Object.MeasureWithDuration(call.Object, "aneventname", action);

        result.Should().Be(1);
        wasCalled.Should().BeTrue();
        recorder.Verify(rec => rec.Measure(call.Object, "aneventname", It.Is<Dictionary<string, object>>(dic =>
            dic.Count == 2
            && (string)dic["akey"] == "avalue"
            && dic.ContainsKey("DurationInMS")
        )));
    }
}