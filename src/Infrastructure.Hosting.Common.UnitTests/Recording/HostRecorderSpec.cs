using Common;
using Common.Recording;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Infrastructure.Common.Recording;
using Infrastructure.Hosting.Common.Recording;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.Recording;

[Trait("Category", "Unit")]
public sealed class HostRecorderSpec : IDisposable
{
    private readonly Mock<ICallContext> _call;
    private readonly MockLogger _logger;
    private readonly HostRecorder _recorder;

    public HostRecorderSpec()
    {
        _logger = new MockLogger();
        var crasher = new Mock<ICrashReporter>();
        var auditor = new Mock<IAuditReporter>();
        var measurer = new Mock<IMetricReporter>();
        var follower = new Mock<IUsageReporter>();
        _call = new Mock<ICallContext>();
        _call.Setup(c => c.HostRegion)
            .Returns(DatacenterLocations.AustraliaEast);

        _recorder = new HostRecorder(_logger, new RecorderOptions(), crasher.Object,
            auditor.Object, measurer.Object, follower.Object);
        _logger.Reset();
    }

    ~HostRecorderSpec()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _recorder.Dispose();
        }
    }

    [Fact]
    public void WhenTraceInformationWithNoMessage_ThenLogs()
    {
        _recorder.TraceInformation(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTraceInformationWithNoArgs_ThenTraces()
    {
        _recorder.TraceInformation(null, "amessagetemplate");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("amessagetemplate");
    }

    [Fact]
    public void WhenTraceInformationWithNoCall_ThenTraces()
    {
        _recorder.TraceInformation(null, "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("amessagetemplateanarg1anarg2");
    }

    [Fact]
    public void WhenTraceInformationWithAnonymousCall_ThenTraces()
    {
        _recorder.TraceInformation(_call.Object, "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be(
                $"Request: {CallConstants.UncorrelatedCallId}, (by anonymous, in aus)  amessagetemplateanarg1anarg2");
    }

    [Fact]
    public void WhenTraceInformationWithAuthenticatedCall_ThenTraces()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");
        _call.Setup(x => x.CallId)
            .Returns("acallid");

        _recorder.TraceInformation(_call.Object, "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Request: acallid, (by acallerid, in aus)  amessagetemplateanarg1anarg2");
    }

    [Fact]
    public void WhenTraceDebugWithNoMessage_ThenLogs()
    {
        _recorder.TraceDebug(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTraceWarningWithNoMessage_ThenLogs()
    {
        _recorder.TraceWarning(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTraceInformationWithAuthenticatedCallAndNoMessage_ThenLogs()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");
        _call.Setup(x => x.CallId)
            .Returns("acallid");

        _recorder.TraceInformation(_call.Object, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTraceErrorWithCallAndExceptionButNoMessage_ThenLogs()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");
        _call.Setup(x => x.CallId)
            .Returns("acallid");
        var exception = new Exception("amessage");

        _recorder.TraceError(_call.Object, exception, string.Empty);

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Error);
        item.Message.Should().Be("Request: acallid, (by acallerid, in aus)  ");
        item.Exception.Should().Be(exception);
    }

    [Fact]
    public void WhenTraceErrorWithNoMessage_ThenLogs()
    {
        _recorder.TraceError(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTraceErrorWithCall_ThenLogs()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");
        _call.Setup(x => x.CallId)
            .Returns("acallid");
        var exception = new Exception("amessage");

        _recorder.TraceError(_call.Object, exception, "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Error);
        item.Message.Should().Be("Request: acallid, (by acallerid, in aus)  amessagetemplateanarg1anarg2");
        item.Exception.Should().Be(exception);
    }

    [Fact]
    public void WhenAuditWithNoCode_ThenLogs()
    {
        _recorder.Audit(null, string.Empty, "amessagetemplate");

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenAuditWithoutCaller_ThenTraces()
    {
        _recorder.Audit(null, "anAuditCode", "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Audit: anAuditCode, with message: amessagetemplateanarg1anarg2");
    }

    [Fact]
    public void WhenAuditWithCaller_ThenTraces()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");

        _recorder.Audit(_call.Object, "anAuditCode", "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        _logger.Items.Count.Should().Be(2);
        _logger.Items[0].Level.Should().Be(LogLevel.Information);
        _logger.Items[0].Message.Should()
            .Be(
                "Request: uncorrelated, (by acallerid, in aus)  Audit: anAuditCode, against: acallerid, with message: amessagetemplateanarg1anarg2");
        _logger.Items[1].Level.Should().Be(LogLevel.Information);
        _logger.Items[1].Message.Should()
            .Be(
                "Request: uncorrelated, (by acallerid, in aus)  Usage: Audited, for: acallerid, with properties: <\"UserId\": acallerid, \"Code\": anauditcode, \"Component\": >");
    }

    [Fact]
    public void WhenAuditAgainstWithNoCode_ThenLogs()
    {
        _recorder.AuditAgainst(null, "anagainstid", string.Empty, "amessagetemplate");

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenAuditAgainst_ThenTraces()
    {
        _recorder.AuditAgainst(null, "anid", "anAuditCode", "amessagetemplate{Arg1}{Arg2}", "anarg1", "anarg2");

        _logger.Items.Count.Should().Be(2);
        _logger.Items[0].Level.Should().Be(LogLevel.Information);
        _logger.Items[0].Message.Should()
            .Be("Audit: anAuditCode, against: anid, with message: amessagetemplateanarg1anarg2");
        _logger.Items[1].Level.Should().Be(LogLevel.Information);
        _logger.Items[1].Message.Should()
            .Be(
                "Usage: Audited, for: anid, with properties: <\"UserId\": anid, \"Code\": anauditcode, \"Component\": >");
    }

    [Fact]
    public void WhenCrash_ThenTraces()
    {
        var exception = new Exception("amessage");
        _recorder.Crash(null, CrashLevel.Critical, exception);

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Critical);
        item.Message.Should().Be("Crash (Critical): amessage");
        item.Exception.Should().Be(exception);
    }

    [Fact]
    public void WhenCrashWithMessage_ThenTraces()
    {
        var exception = new Exception("amessage");
        _recorder.Crash(null, CrashLevel.Critical, exception, "amessagetemplate{Arg1}{Arg2}", "anarg1",
            "anarg2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Critical);
        item.Message.Should().Be("Crash (Critical): amessagetemplateanarg1anarg2");
        item.Exception.Should().Be(exception);
    }

    [Fact]
    public void WhenMeasureWithNoEvent_ThenLogs()
    {
        _recorder.Measure(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenMeasure_ThenTraces()
    {
        _recorder.Measure(null, "aneventname", new Dictionary<string, object>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        });

        _logger.Items.Count.Should().Be(2);
        _logger.Items[0].Level.Should().Be(LogLevel.Information);
        _logger.Items[0].Message.Should()
            .Be("Measure: aneventname, with properties: <\"aname1\": avalue1, \"aname2\": avalue2>");
        _logger.Items[1].Level.Should().Be(LogLevel.Information);
        _logger.Items[1].Message.Should()
            .Be(
                "Usage: Measured, with properties: <\"aname1\": avalue1, \"aname2\": avalue2, \"Metric\": aneventname>");
    }

    [Fact]
    public void WhenTrackUsageWithNoEvent_ThenLogs()
    {
        _recorder.TrackUsage(null, string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTrackUsageWithoutAdditional_ThenTraces()
    {
        _recorder.TrackUsage(null, "aneventname");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Usage: aneventname, with properties: none");
    }

    [Fact]
    public void WhenTrackUsage_ThenTraces()
    {
        _recorder.TrackUsage(null, "aneventname", new Dictionary<string, object>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        });

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Usage: aneventname, with properties: <\"aname1\": avalue1, \"aname2\": avalue2>");
    }

    [Fact]
    public void WhenTrackUsageForWithNoEvent_ThenLogs()
    {
        _recorder.TrackUsageFor(null, "aforid", string.Empty);

        _logger.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenTrackUsageFor_ThenTraces()
    {
        _recorder.TrackUsageFor(null, "aforid", "aneventname", new Dictionary<string, object>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        });

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be(
                "Usage: aneventname, for: aforid, with properties: <\"aname1\": avalue1, \"aname2\": avalue2, \"Component\": >");
    }

    [Fact]
    public void WhenTrackUsageForWithCaller_ThenTraces()
    {
        _call.Setup(x => x.CallerId)
            .Returns("acallerid");

        _recorder.TrackUsageFor(_call.Object, "aforid", "aneventname", new Dictionary<string, object>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        });

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be(
                $"Request: {CallConstants.UncorrelatedCallId}, (by acallerid, in aus)  Usage: aneventname, for: aforid, with properties: <\"aname1\": avalue1, \"aname2\": avalue2, \"Component\": >");
    }

    [Fact]
    public void WhenTrackUsageForWithUnSerializableAdditionalProperties_ThenTraces()
    {
        _recorder.TrackUsageFor(null, "aforid", "aneventname", new Dictionary<string, object>
        {
            { "aname1", "avalue1" },
            { "aname2", Identifier.Create("avalue2") },
            { "aname3", new TestObject() },
            { "aname4", "{avalue4}" }
        });

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be(
                "Usage: aneventname, for: aforid, with properties: <\"aname1\": avalue1, \"aname2\": avalue2, \"aname3\": atestobjectvalue, \"aname4\": {{avalue4}}, \"Component\": >");
    }
}