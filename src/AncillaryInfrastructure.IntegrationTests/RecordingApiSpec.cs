using System.Text.Json;
using ApiHost1;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class RecordingApiSpec : WebApiSpec<Program>
{
    private readonly StubRecorder _recorder;

    public RecordingApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
        _recorder = setup.GetRequiredService<IRecorder>().As<StubRecorder>();
        _recorder.Reset();
    }

    [Fact]
    public async Task WhenRecordUseWithNoAdditional_ThenRecords()
    {
        var request = new RecordUseRequest
        {
            EventName = "aneventname",
            Additional = null
        };
        await Api.PostAsync(request, req => req.SetHmacAuth(request, "asecret"));

        _recorder.LastUsageEventName.Should().Be("aneventname");
        _recorder.LastUsageAdditional.Should().BeNull();
    }

    [Fact]
    public async Task WhenRecordUse_ThenRecords()
    {
        var request = new RecordUseRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname1", "avalue" },
                { "aname2", 25 },
                { "aname3", true }
            }
        };
        await Api.PostAsync(request, req => req.SetHmacAuth(request, "asecret"));

        _recorder.LastUsageEventName.Should().Be("aneventname");
        _recorder.LastUsageAdditional!.Count.Should().Be(3);
        _recorder.LastUsageAdditional!["aname1"].As<JsonElement>().GetString().Should().Be("avalue");
        _recorder.LastUsageAdditional!["aname2"].As<JsonElement>().GetInt32().Should().Be(25);
        _recorder.LastUsageAdditional!["aname3"].As<JsonElement>().GetBoolean().Should().Be(true);
    }

    [Fact]
    public async Task WhenRecordMeasure_ThenRecords()
    {
        var request = new RecordMeasureRequest
        {
            EventName = "aneventname",
            Additional = new Dictionary<string, object?>
            {
                { "aname1", "avalue" },
                { "aname2", 25 },
                { "aname3", true }
            }
        };
        await Api.PostAsync(request, req => req.SetHmacAuth(request, "asecret"));

        _recorder.LastMeasureEventName.Should().Be("aneventname");
        _recorder.LastMeasureAdditional!.Count.Should().Be(3);
        _recorder.LastMeasureAdditional!["aname1"].As<JsonElement>().GetString().Should().Be("avalue");
        _recorder.LastMeasureAdditional!["aname2"].As<JsonElement>().GetInt32().Should().Be(25);
        _recorder.LastMeasureAdditional!["aname3"].As<JsonElement>().GetBoolean().Should().Be(true);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IRecorder, StubRecorder>();
    }
}