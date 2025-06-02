using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Recording;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using IntegrationTesting.Website.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace WebsiteHost.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("WEBSITE")]
public class RecordingApiSpec : WebsiteSpec<Program, ApiHost1.Program>
{
    private readonly StubRecorder _recorder;

    public RecordingApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        _recorder = setup.GetRequiredService<IRecorder>().As<StubRecorder>();
        _recorder.Reset();
    }

    [Fact]
    public async Task WhenRecordPageView_ThenRecordsUsage()
    {
        var request = new RecordPageViewRequest
        {
            Path = "apath"
        };
        await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        _recorder.LastUsageEventName.Should().Be(UsageConstants.Events.Web.WebPageVisit);
        _recorder.LastUsageAdditional!.Count.Should().Be(6);
        _recorder.LastUsageAdditional![UsageConstants.Properties.Path].As<string>().Should().Be("apath");
        _recorder.LastUsageAdditional![UsageConstants.Properties.Timestamp].As<DateTime>().Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _recorder.LastUsageAdditional![UsageConstants.Properties.IpAddress].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.UserAgent].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.ReferredBy].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.Component].As<string>().Should()
            .Be(UsageConstants.Components.BackEndForFrontEndWebHost);
    }

    [Fact]
    public async Task WhenRecordUsage_ThenRecordsUsage()
    {
        var request = new RecordUseRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname", "avalue" }
            }
        };
        await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        _recorder.LastUsageEventName.Should().Be("aneventname");
        _recorder.LastUsageAdditional!.Count.Should().Be(6);
        _recorder.LastUsageAdditional!["aname"].As<JsonElement>().GetString().Should().Be("avalue");
        _recorder.LastUsageAdditional![UsageConstants.Properties.Timestamp].As<DateTime>().Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _recorder.LastUsageAdditional![UsageConstants.Properties.IpAddress].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.UserAgent].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.ReferredBy].As<string>().Should().Be("unknown");
        _recorder.LastUsageAdditional![UsageConstants.Properties.Component].As<string>().Should()
            .Be(UsageConstants.Components.BackEndForFrontEndWebHost);
    }

    [Fact]
    public async Task WhenRecordCrash_ThenRecordsCrash()
    {
        var request = new RecordCrashRequest
        {
            Message = "amessage"
        };
        await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        _recorder.LastCrashLevel.Should().Be(CrashLevel.Critical);
        _recorder.LastCrashException.Should().BeOfType<Exception>();
        _recorder.LastCrashException!.Message.Should().EndWith("amessage");
    }

    [Fact]
    public async Task WhenRecordTrace_ThenRecordsTrace()
    {
        var request = new RecordTraceRequest
        {
            Level = RecorderTraceLevel.Warning.ToString(),
            MessageTemplate = "amessage {aparam}",
            Arguments = new List<string>
            {
                "avalue"
            }
        };
        await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        _recorder.LastTraceMessages.Should().Contain(msg =>
            msg.Message == "amessage {aparam}"
            && msg.Level == StubRecorderTraceLevel.Warning
            && msg.Arguments!.Length == 1 && (string)msg.Arguments[0] == "avalue");
    }

    [Fact]
    public async Task WhenRecordMeasurement_ThenRecordsMeasurement()
    {
        var request = new RecordMeasureRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname", "avalue" }
            }
        };
        await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        _recorder.LastMeasureEventName.Should().Be("aneventname");
        _recorder.LastMeasureAdditional!.Count.Should().Be(7);
        _recorder.LastMeasureAdditional!["aname"].As<JsonElement>().GetString().Should().Be("avalue");
        _recorder.LastMeasureAdditional![UsageConstants.Properties.ForId].As<string>().Should()
            .Be(CallerConstants.AnonymousUserId);
        _recorder.LastMeasureAdditional![UsageConstants.Properties.Timestamp].As<DateTime>().Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _recorder.LastMeasureAdditional![UsageConstants.Properties.IpAddress].As<string>().Should().Be("unknown");
        _recorder.LastMeasureAdditional![UsageConstants.Properties.UserAgent].As<string>().Should().Be("unknown");
        _recorder.LastMeasureAdditional![UsageConstants.Properties.ReferredBy].As<string>().Should().Be("unknown");
        _recorder.LastMeasureAdditional![UsageConstants.Properties.Component].As<string>().Should()
            .Be(UsageConstants.Components.BackEndForFrontEndWebHost);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IRecorder, StubRecorder>();
    }
}