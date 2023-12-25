using Common;
using Common.Recording;
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
    private readonly MockLogger _logger;
    private readonly HostRecorder _recorder;

    public HostRecorderSpec()
    {
        _logger = new MockLogger();
        var crasher = new Mock<ICrashReporter>();
        var auditor = new Mock<IAuditReporter>();
        var measurer = new Mock<IMetricReporter>();
        var follower = new Mock<IUsageReporter>();
        _recorder = new HostRecorder(_logger, new RecorderOptions(), crasher.Object,
            auditor.Object, measurer.Object, follower.Object);
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
    public void WhenTraceInformationWithNoParameters_ThenLogs()
    {
        _recorder.TraceInformation(null, string.Empty);

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be($"Request: {CallConstants.UncorrelatedCallId} By: {CallConstants.UnknownCallerId}");
    }

    [Fact]
    public void WhenTraceInformationWithTemplateAndNoArgs_ThenLogs()
    {
        _recorder.TraceInformation(null, "amessagetemplate");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be($"Request: {CallConstants.UncorrelatedCallId} By: {CallConstants.UnknownCallerId}: amessagetemplate");
    }

    [Fact]
    public void WhenTraceInformationWithTemplateAndArgs_ThenLogs()
    {
        _recorder.TraceInformation(null, "{Value1} {Value2}", "avalue1", "avalue2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should()
            .Be($"Request: {CallConstants.UncorrelatedCallId} By: {CallConstants.UnknownCallerId}: avalue1 avalue2");
    }

    [Fact]
    public void WhenTraceInformationWithCallTemplateAndArgs_ThenLogs()
    {
        var call = Mock.Of<ICallContext>(call => call.CallerId == "acallerid" && call.CallId == "acallid");
        _recorder.TraceInformation(call, "{Value1} {Value2}", "avalue1", "avalue2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Request: acallid By: acallerid: avalue1 avalue2");
    }

    [Fact]
    public void WhenTraceInformationWithCallAndNoTemplateNorArgs_ThenLogs()
    {
        var call = Mock.Of<ICallContext>(call => call.CallerId == "acallerid" && call.CallId == "acallid");
        _recorder.TraceInformation(call, string.Empty);

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Information);
        item.Message.Should().Be("Request: acallid By: acallerid");
    }

    [Fact]
    public void WhenTraceErrorWithCallAndNoTemplateNorArgs_ThenLogs()
    {
        var call = Mock.Of<ICallContext>(call => call.CallerId == "acallerid" && call.CallId == "acallid");
        var exception = new Exception("amessage");
        _recorder.TraceError(call, exception, string.Empty);

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Error);
        item.Message.Should().Be("Request: acallid By: acallerid");
        item.Exception.Should().Be(exception);
    }

    [Fact]
    public void WhenTraceErrorWithCallAndTemplateAndArgs_ThenLogs()
    {
        var call = Mock.Of<ICallContext>(call => call.CallerId == "acallerid" && call.CallId == "acallid");
        var exception = new Exception("amessage");
        _recorder.TraceError(call, exception, "{Value1} {Value2}", "avalue1", "avalue2");

        var item = _logger.Items.Single();
        item.Level.Should().Be(LogLevel.Error);
        item.Message.Should().Be("Request: acallid By: acallerid: avalue1 avalue2");
        item.Exception.Should().Be(exception);
    }
}