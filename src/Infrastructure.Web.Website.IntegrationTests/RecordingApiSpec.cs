using System.Text.Json;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Recording;
using FluentAssertions;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using WebsiteHost;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class RecordingApiSpec : WebApiSpec<Program>
{
    private readonly StubRecorder _recorder;

    public RecordingApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _recorder = setup.GetRequiredService<IRecorder>().As<StubRecorder>();
        _recorder.Reset();
    }

    [Fact]
    public async Task WhenRecordPageView_ThenRecordsUsage()
    {
        await Api.PostAsync(new RecordPageViewRequest
        {
            Path = "apath"
        });

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
        await Api.PostAsync(new RecordUseRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname", "avalue" }
            }
        });

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
        await Api.PostAsync(new RecordCrashRequest
        {
            Message = "amessage"
        });

        _recorder.LastCrashLevel.Should().Be(CrashLevel.Critical);
        _recorder.LastCrashException.Should().BeOfType<Exception>();
        _recorder.LastCrashException!.Message.Should().EndWith("amessage");
    }

    [Fact]
    public async Task WhenRecordTrace_ThenRecordsTrace()
    {
        await Api.PostAsync(new RecordTraceRequest
        {
            Level = RecorderTraceLevel.Warning.ToString(),
            MessageTemplate = "amessage {aparam}",
            Arguments = new List<string>
            {
                "avalue"
            }
        });

        _recorder.LastTraceLevel.Should().Be(StubRecorderTraceLevel.Warning);
        _recorder.LastTraceMessageTemplate.Should().Be("amessage {aparam}");
        _recorder.LastTraceArguments.Should().ContainInOrder("avalue");
    }

    [Fact]
    public async Task WhenRecordMeasurement_ThenRecordsMeasurement()
    {
        await Api.PostAsync(new RecordMeasureRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname", "avalue" }
            }
        });

        _recorder.LastMeasureEventName.Should().Be("aneventname");
        _recorder.LastMeasureAdditional!.Count.Should().Be(6);
        _recorder.LastMeasureAdditional!["aname"].As<JsonElement>().GetString().Should().Be("avalue");
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
        services.RegisterUnshared<IRecorder, StubRecorder>();
    }
}