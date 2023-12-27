using Application.Persistence.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using StringExtensions = Common.Extensions.StringExtensions;

namespace Infrastructure.Worker.Api.IntegrationTests;

public abstract class DeliverUsageSpecBase<TSetup> : ApiWorkerSpec<TSetup>
    where TSetup : class, IApiWorkerSpec
{
    private readonly StubServiceClient _serviceClient;

    protected DeliverUsageSpecBase(TSetup setup) : base(setup, OverrideDependencies)
    {
        setup.QueueStore.DestroyAllAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None).GetAwaiter()
            .GetResult();
        _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
        _serviceClient.Reset();
    }

    [Fact]
    public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    {
        await Setup.QueueStore.PushAsync(DeliverUsageRelayWorker.QueueName, "aninvalidusagemessage",
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

        (await Setup.QueueStore.CountAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None))
            .Should().Be(0);
        _serviceClient.LastPostedMessage.Should().BeNone();
    }

    [Fact]
    public async Task WhenMessageQueuedContaining_ThenApiCalled()
    {
        var message = StringExtensions.ToJson(new UsageMessage
        {
            ForId = "aforid",
            EventName = "aneventname"
        })!;
        await Setup.QueueStore.PushAsync(DeliverUsageRelayWorker.QueueName, message, CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

        (await Setup.QueueStore.CountAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None))
            .Should().Be(0);
        _serviceClient.LastPostedMessage.Value.Should()
            .BeEquivalentTo(new DeliverUsageRequest { Message = message });
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IServiceClient, StubServiceClient>();
    }
}