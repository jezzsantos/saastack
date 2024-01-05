using Application.Persistence.Shared.ReadModels;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Infrastructure.Workers.Api.Workers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Worker.Api.IntegrationTests.AWSLambdas;

[UsedImplicitly]
public class AWSLambdasApiSpec
{
    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverUsageSpec : DeliverUsageSpecBase<AWSLambdaHostSetup>
    {
        public DeliverUsageSpec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }

    // public class DeliverUsageSpec : ApiWorkerSpec<AWSLambdaHostSetup>
    // {
    //     private readonly StubServiceClient _serviceClient;
    //
    //     public DeliverUsageSpec(AWSLambdaHostSetup setup) : base(setup, OverrideDependencies)
    //     {
    //         setup.QueueStore.DestroyAllAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None).GetAwaiter()
    //             .GetResult();
    //         _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
    //         _serviceClient.Reset();
    //     }
    //
    //     [Fact]
    //     public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    //     {
    //         await Setup.QueueStore.PushAsync(DeliverUsageRelayWorker.QueueName, "aninvalidusagemessage",
    //             CancellationToken.None);
    //
    //         Setup.WaitForQueueProcessingToComplete();
    //
    //         (await Setup.QueueStore.CountAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None))
    //             .Should().Be(0);
    //         _serviceClient.LastPostedMessage.Should().BeNone();
    //     }
    //
    //     [Fact]
    //     public async Task WhenMessageQueuedContaining_ThenApiCalled()
    //     {
    //         var message = new UsageMessage
    //         {
    //             ForId = "aforid",
    //             EventName = "aneventname"
    //         }.ToJson()!;
    //         await Setup.QueueStore.PushAsync(DeliverUsageRelayWorker.QueueName, message, CancellationToken.None);
    //
    //         Setup.WaitForQueueProcessingToComplete();
    //
    //         (await Setup.QueueStore.CountAsync(DeliverUsageRelayWorker.QueueName, CancellationToken.None))
    //             .Should().Be(0);
    //         _serviceClient.LastPostedMessage.Value.Should()
    //             .BeEquivalentTo(new DeliverUsageRequest { Message = message });
    //     }
    //
    //     private static void OverrideDependencies(IServiceCollection services)
    //     {
    //         services.AddSingleton<IServiceClient, StubServiceClient>();
    //     }
    // }

    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverAuditSpec : ApiWorkerSpec<AWSLambdaHostSetup>
    {
        private readonly StubServiceClient _serviceClient;

        public DeliverAuditSpec(AWSLambdaHostSetup setup) : base(setup, OverrideDependencies)
        {
            setup.QueueStore.DestroyAllAsync(DeliverAuditRelayWorker.QueueName, CancellationToken.None).GetAwaiter()
                .GetResult();
            _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
            _serviceClient.Reset();
        }

        [Fact]
        public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
        {
            await Setup.QueueStore.PushAsync(DeliverAuditRelayWorker.QueueName, "aninvalidusagemessage",
                CancellationToken.None);

            Setup.WaitForQueueProcessingToComplete();

            (await Setup.QueueStore.CountAsync(DeliverAuditRelayWorker.QueueName, CancellationToken.None))
                .Should().Be(0);
            _serviceClient.LastPostedMessage.Should().BeNone();
        }

        [Fact]
        public async Task WhenMessageQueuedContaining_ThenApiCalled()
        {
            var message = new AuditMessage
            {
                AuditCode = "anauditcode"
            }.ToJson()!;
            await Setup.QueueStore.PushAsync(DeliverAuditRelayWorker.QueueName, message, CancellationToken.None);

            Setup.WaitForQueueProcessingToComplete();

            (await Setup.QueueStore.CountAsync(DeliverAuditRelayWorker.QueueName, CancellationToken.None))
                .Should().Be(0);
            _serviceClient.LastPostedMessage.Value.Should()
                .BeEquivalentTo(new DeliverAuditRequest { Message = message });
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IServiceClient, StubServiceClient>();
        }
    }
}