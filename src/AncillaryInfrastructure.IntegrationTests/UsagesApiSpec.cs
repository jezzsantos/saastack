using AncillaryInfrastructure.IntegrationTests.Stubs;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
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
[Collection("API")]
public class UsagesApiSpec : WebApiSpec<Program>
{
    private readonly StubUsageDeliveryService _usageDeliveryService;
    private readonly IUsageMessageQueue _usageMessageQueue;

    public UsagesApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _usageDeliveryService = setup.GetRequiredService<IUsageDeliveryService>().As<StubUsageDeliveryService>();
        _usageDeliveryService.Reset();
        _usageMessageQueue = setup.GetRequiredService<IUsageMessageQueue>();
        _usageMessageQueue.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task WhenDeliverUsage_ThenDelivers()
    {
        var request = new DeliverUsageRequest
        {
            Message = new UsageMessage
            {
                MessageId = "amessageid",
                CallId = "acallid",
                CallerId = "acallerid",
                EventName = "aneventname",
                ForId = "aforid"
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsDelivered.Should().BeTrue();
        _usageDeliveryService.LastEventName.Should().Be("aneventname");
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllUsagesAndNone_ThenDoesNotDrainAny()
    {
        var request = new DrainAllUsagesRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _usageDeliveryService.LastEventName.Should().BeNone();
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
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _usageDeliveryService.AllEventNames.Count.Should().Be(3);
        _usageDeliveryService.AllEventNames.Should().ContainInOrder("aneventname1", "aneventname2", "aneventname3");
    }
#endif

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IUsageDeliveryService, StubUsageDeliveryService>();
    }
}