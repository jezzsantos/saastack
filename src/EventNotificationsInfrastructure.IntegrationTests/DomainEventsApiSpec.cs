using ApiHost1;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using EventNotificationsInfrastructure.IntegrationTests.Stubs;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;
#if TESTINGONLY
using Domain.Events.Shared.TestingOnly;
#endif

namespace EventNotificationsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class DomainEventsApiSpec : WebApiSpec<Program>
{
    private readonly StubDomainEventingConsumerService _stubConsumerService;

    public DomainEventsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        var domainEventingMessageBusTopic = setup.GetRequiredService<IDomainEventingMessageBusTopic>();
#if TESTINGONLY
        domainEventingMessageBusTopic.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
#endif
        _stubConsumerService = setup.GetRequiredService<IDomainEventingConsumerService>()
            .As<StubDomainEventingConsumerService>();
        _stubConsumerService.Reset();
    }

    [Fact]
    public async Task WhenNotifyDomainEvent_ThenNotifies()
    {
#if TESTINGONLY
#pragma warning disable CS0618 // Type or member is obsolete
        var @event = new Happened
        {
            RootId = "arootid",
            OccurredUtc = DateTime.UtcNow,
            Message1 = "amessage1"
        };
        var request = new NotifyDomainEventRequest
        {
            SubscriptionName = "asubscriptionname",
            Message = new DomainEventingMessage
            {
                MessageId = "amessageid",
                TenantId = null,
                CallId = "acallid",
                CallerId = "acallerid",
                Event = new EventStreamChangeEvent
                {
                    Id = "aneventid",
                    RootAggregateType = "anaggregatetype",
                    Data = @event.ToEventJson(),
                    Version = 1,
                    Metadata = new EventMetadata(typeof(Happened).AssemblyQualifiedName!),
                    EventType = nameof(Happened),
                    LastPersistedAtUtc = default,
                    StreamName = "astreamname"
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
        _stubConsumerService.LastEventId.Should().Be("aneventid");
        _stubConsumerService.LastEventSubscriptionName.Should().Be("asubscriptionname");
#pragma warning restore CS0618 // Type or member is obsolete
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IDomainEventingConsumerService, StubDomainEventingConsumerService>();
    }
}