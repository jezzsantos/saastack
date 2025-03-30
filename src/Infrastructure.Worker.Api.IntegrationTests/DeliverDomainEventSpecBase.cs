using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using StringExtensions = Common.Extensions.StringExtensions;

namespace Infrastructure.Worker.Api.IntegrationTests;

public abstract class DeliverDomainEventSpecBase<TSetup> : ApiWorkerSpec<TSetup>
    where TSetup : class, IApiWorkerSpec
{
    private readonly StubServiceClientFactory _serviceClientFactory;

    protected DeliverDomainEventSpecBase(TSetup setup) : base(setup, OverrideDependencies)
    {
#if TESTINGONLY
        setup.MessageBusStore.DestroyAllAsync(WorkerConstants.MessageBuses.Topics.DomainEvents, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
#endif
        _serviceClientFactory = setup.GetRequiredService<IServiceClient>().As<StubServiceClientFactory>();
        _serviceClientFactory.Reset();
    }

    [Fact]
    public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    {
        await Setup.MessageBusStore.SendAsync(WorkerConstants.MessageBuses.Topics.DomainEvents, "aninvalidmessage",
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.MessageBusStore.CountAsync(WorkerConstants.MessageBuses.Topics.DomainEvents,
                WorkerConstants.MessageBuses.SubscriberHosts.ApiHost1, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClientFactory.LastPostedMessage.Should().BeNone();
    }

    [Fact]
    public async Task WhenMessageQueued_ThenApiCalled()
    {
        var message = StringExtensions.ToJson(new DomainEventingMessage
        {
            TenantId = "anorganizationid",
            Event = new EventStreamChangeEvent
            {
                Id = "anid",
                RootAggregateType = nameof(String),
                Data = new TestDomainEvent
                {
                    RootId = "aneventid"
                }.ToEventJson(),
                Version = 1,
                Metadata = new EventMetadata("unknowntype"),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        })!;
        await Setup.MessageBusStore.SendAsync(WorkerConstants.MessageBuses.Topics.DomainEvents, message,
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.MessageBusStore.CountAsync(WorkerConstants.MessageBuses.Topics.DomainEvents,
                WorkerConstants.MessageBuses.SubscriberHosts.ApiHost1, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClientFactory.LastPostedMessage.Value.Should()
            .BeEquivalentTo(new NotifyDomainEventRequest { Message = message });
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IServiceClientFactory, StubServiceClientFactory>();
    }
}