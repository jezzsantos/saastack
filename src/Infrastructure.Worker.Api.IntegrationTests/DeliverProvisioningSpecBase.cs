using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using StringExtensions = Common.Extensions.StringExtensions;

namespace Infrastructure.Worker.Api.IntegrationTests;

public abstract class DeliverProvisioningSpecBase<TSetup> : ApiWorkerSpec<TSetup>
    where TSetup : class, IApiWorkerSpec
{
    private readonly StubServiceClient _serviceClient;

    protected DeliverProvisioningSpecBase(TSetup setup) : base(setup, OverrideDependencies)
    {
#if TESTINGONLY
        setup.QueueStore.DestroyAllAsync(WorkerConstants.Queues.Provisionings, CancellationToken.None).GetAwaiter()
            .GetResult();
#endif
        _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
        _serviceClient.Reset();
    }

    [Fact]
    public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    {
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Provisionings, "aninvalidmessage",
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Provisionings, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Should().BeNone();
    }

    [Fact]
    public async Task WhenMessageQueued_ThenApiCalled()
    {
        var message = StringExtensions.ToJson(new ProvisioningMessage
        {
            TenantId = "anorganizationid",
            Settings = new Dictionary<string, TenantSetting>
            {
                { "aname1", new TenantSetting("avalue") },
                { "aname2", new TenantSetting(99) },
                { "aname3", new TenantSetting(true) }
            }
        })!;
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Provisionings, message, CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Provisionings, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Value.Should()
            .BeEquivalentTo(new NotifyProvisioningRequest { Message = message });
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IServiceClient, StubServiceClient>();
    }
}