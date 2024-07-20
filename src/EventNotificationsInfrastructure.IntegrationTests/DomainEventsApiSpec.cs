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
    private readonly StubDomainEventConsumerService _consumerService;

    public DomainEventsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        var domainEventingMessageBusTopic = setup.GetRequiredService<IDomainEventingMessageBusTopic>();
#if TESTINGONLY
        domainEventingMessageBusTopic.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
#endif
        _consumerService = setup.GetRequiredService<IDomainEventConsumerService>().As<StubDomainEventConsumerService>();
    }

    [Fact]
    public async Task WhenNotifyDomainEventing_ThenNotifies()
    {
#if TESTINGONLY
        var @event = new Happened
        {
            RootId = "arootid",
            OccurredUtc = DateTime.UtcNow
        };
        var request = new NotifyDomainEventRequest
        {
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
        _consumerService.LastEventId.Should().Be("aneventid");
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IDomainEventConsumerService, StubDomainEventConsumerService>();
    }
}