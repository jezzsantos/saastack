using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests;

public abstract class DeliverAuditSpecBase<TSetup> : ApiWorkerSpec<TSetup>
    where TSetup : class, IApiWorkerSpec
{
    private readonly StubServiceClient _serviceClient;

    protected DeliverAuditSpecBase(TSetup setup) : base(setup, OverrideDependencies)
    {
#if TESTINGONLY
        setup.QueueStore.DestroyAllAsync(WorkerConstants.Queues.Audits, CancellationToken.None).GetAwaiter()
            .GetResult();
#endif
        _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
        _serviceClient.Reset();
    }

    [Fact]
    public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    {
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Audits, "aninvalidmessage",
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();
#if TESTINGONLY

        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Audits, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Should().BeNone();
    }

    [Fact]
    public async Task WhenMessageQueued_ThenApiCalled()
    {
        var message = new AuditMessage
        {
            TenantId = "atenantid",
            AuditCode = "anauditcode"
        }.ToJson()!;
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Audits, message, CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Audits, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Value.Should()
            .BeEquivalentTo(new DeliverAuditRequest { Message = message });
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IServiceClient, StubServiceClient>();
    }
}