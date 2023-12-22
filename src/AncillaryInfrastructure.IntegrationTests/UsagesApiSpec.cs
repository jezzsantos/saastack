using AncillaryInfrastructure.IntegrationTests.Stubs;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class UsagesApiSpec : WebApiSpec<Program>
{
    private readonly IUsageMessageQueueRepository _usageMessageQueue;
    private readonly StubUsageReportingService _usageReportingService;

    public UsagesApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
        _usageReportingService = setup.GetRequiredService<IUsageReportingService>().As<StubUsageReportingService>();
        _usageReportingService.Reset();
        _usageMessageQueue = setup.GetRequiredService<IUsageMessageQueueRepository>();
        _usageMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task WhenDeliverUsage_ThenDelivers()
    {
        var result = await Api.PostAsync(new DeliverUsageRequest
        {
            Message = new UsageMessage
            {
                CallId = "acallid",
                CallerId = "acallerid",
                EventName = "aneventname",
                ForId = "aforid",
                MessageId = "amessageid"
            }.ToJson()!
        });

        result.Content.Value.IsDelivered.Should().BeTrue();
        _usageReportingService.LastEventName.Should().Be("aneventname");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAndNone_ThenDoesNotDrainAny()
    {
        var request = new DrainAllUsagesRequest();
        await Api.PostAsync(request, req => req.SetHmacAuth(request, "asecret"));

        _usageReportingService.LastEventName.Should().BeNone();
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAndSome_ThenDrains()
    {
        var call = CallContext.CreateCustom("acallid", "acallerid", "atenantid");
        await _usageMessageQueue.PushAsync(call, new UsageMessage
        {
            MessageId = "amessageid1",
            ForId = "aforid1",
            EventName = "aneventname1"
        }, CancellationToken.None);
        await _usageMessageQueue.PushAsync(call, new UsageMessage
        {
            MessageId = "amessageid2",
            ForId = "aforid2",
            EventName = "aneventname2"
        }, CancellationToken.None);
        await _usageMessageQueue.PushAsync(call, new UsageMessage
        {
            MessageId = "amessageid3",
            ForId = "aforid3",
            EventName = "aneventname3"
        }, CancellationToken.None);

        var request = new DrainAllUsagesRequest();
        await Api.PostAsync(request, req => req.SetHmacAuth(request, "asecret"));

        _usageReportingService.AllEventNames.Count.Should().Be(3);
        _usageReportingService.AllEventNames.Should().ContainInOrder("aneventname1", "aneventname2", "aneventname3");
    }
#endif

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IUsageReportingService, StubUsageReportingService>();
    }
}