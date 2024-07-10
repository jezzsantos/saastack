using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using EventNotificationsApplication.Persistence;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace EventNotificationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class DomainEventsApplicationSpec
{
    private readonly DomainEventsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IDomainEventConsumerService> _domainEventConsumerService;
    private readonly Mock<IDomainEventingMessageBusTopic> _domainEventMessageTopic;
    private readonly Mock<IDomainEventRepository> _domainEventRepository;

    public DomainEventsApplicationSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _domainEventRepository = new Mock<IDomainEventRepository>();
        _domainEventMessageTopic = new Mock<IDomainEventingMessageBusTopic>();
        _domainEventConsumerService = new Mock<IDomainEventConsumerService>();
        var eventingSubscriber = new Mock<IDomainEventingSubscriber>();
        eventingSubscriber.Setup(es => es.SubscriptionName)
            .Returns("asubscriptionname");

        _application = new DomainEventsApplication(recorder.Object, _domainEventRepository.Object,
            _domainEventMessageTopic.Object, eventingSubscriber.Object, _domainEventConsumerService.Object);
    }

    [Fact]
    public async Task WhenNotifyDomainEventAsyncAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result =
            await _application.NotifyDomainEventAsync(_caller.Object, "anunknownmessage", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.DomainEventsApplication_InvalidBusMessage.Format(nameof(DomainEventingMessage),
                "anunknownmessage"));
        _domainEventRepository.Verify(
            der => der.AddAsync(It.IsAny<Persistence.ReadModels.DomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyDomainEventAsync_ThenNotifies()
    {
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid",
            RootAggregateType = "anaggregatetype",
            Data = new TestDomainEvent
            {
                RootId = "aneventid"
            }.ToEventJson(),
            Version = 1,
            Metadata = new EventMetadata("unknowntype"),
            EventType = "aneventtype",
            LastPersistedAtUtc = DateTime.UtcNow,
            StreamName = "astreamname"
        };
        var messageAsJson = new DomainEventingMessage
        {
            TenantId = "atenantid",
            Event = changeEvent
        }.ToJson()!;

        var result = await _application.NotifyDomainEventAsync(_caller.Object, messageAsJson, CancellationToken.None);

        result.Should().BeSuccess();
        _domainEventRepository.Verify(
            der => der.AddAsync(It.Is<Persistence.ReadModels.DomainEvent>(de =>
                de.Id == "anid"
                && de.RootAggregateType == "anaggregatetype"
                && de.EventType == "aneventtype"
                && de.Metadata == new EventMetadata("unknowntype")
                && de.Version == 1
                && de.StreamName == "astreamname"
            ), It.IsAny<CancellationToken>()));
        _domainEventConsumerService.Verify(
            ncs => ncs.NotifyAsync(It.Is<EventStreamChangeEvent>(ce =>
                ce.Id == "anid"
                && ce.RootAggregateType == "anaggregatetype"
                && ce.StreamName == "astreamname"
            ), It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY

    [Fact]
    public async Task WhenDrainAllDomainEventsAsyncAndNoneOnQueue_ThenDoesNotDeliver()
    {
        _domainEventMessageTopic.Setup(umr =>
                umr.ReceiveSingleAsync(It.IsAny<string>(),
                    It.IsAny<Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _application.DrainAllDomainEventsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _domainEventMessageTopic.Verify(
            q => q.ReceiveSingleAsync(It.IsAny<string>(),
                It.IsAny<Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()));
        _domainEventConsumerService.Verify(
            ncs => ncs.NotifyAsync(It.IsAny<EventStreamChangeEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDrainAllDomainEventsAsyncAndSomeOnQueue_ThenDeliversAll()
    {
        var changeEvent1 = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = "anaggregatetype1",
            Data = new TestDomainEvent
            {
                RootId = "aneventid1"
            }.ToEventJson(),
            Version = 1,
            Metadata = new EventMetadata("unknowntype1"),
            EventType = "aneventtype1",
            LastPersistedAtUtc = default,
            StreamName = "astreamname1"
        };
        var message1 = new DomainEventingMessage
        {
            TenantId = "atenantid1",
            Event = changeEvent1
        };
        var changeEvent2 = new EventStreamChangeEvent
        {
            Id = "anid2",
            RootAggregateType = "anaggregatetype2",
            Data = new TestDomainEvent
            {
                RootId = "aneventid2"
            }.ToEventJson(),
            Version = 1,
            Metadata = new EventMetadata("unknowntype2"),
            EventType = "aneventtype2",
            LastPersistedAtUtc = default,
            StreamName = "astreamname2"
        };
        var message2 = new DomainEventingMessage
        {
            TenantId = "atenantid2",
            Event = changeEvent2
        };
        var callbackCount = 1;
        _domainEventMessageTopic.Setup(umr =>
                umr.ReceiveSingleAsync(It.IsAny<string>(),
                    It.IsAny<Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback(
                (string _, Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>> action,
                    CancellationToken _) =>
                {
                    if (callbackCount == 1)
                    {
                        action(message1, CancellationToken.None);
                    }

                    if (callbackCount == 2)
                    {
                        action(message2, CancellationToken.None);
                    }
                })
            .Returns((string _, Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>> _,
                CancellationToken _) =>
            {
                callbackCount++;
                return Task.FromResult<Result<bool, Error>>(callbackCount is 1 or 2);
            });

        var result = await _application.DrainAllDomainEventsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _domainEventMessageTopic.Verify(
            q => q.ReceiveSingleAsync(It.IsAny<string>(),
                It.IsAny<Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        _domainEventConsumerService.Verify(
            ncs => ncs.NotifyAsync(changeEvent1, It.IsAny<CancellationToken>()));
        _domainEventConsumerService.Verify(urs => urs.NotifyAsync(changeEvent2, It.IsAny<CancellationToken>()));
        _domainEventConsumerService.Verify(
            ncs => ncs.NotifyAsync(It.IsAny<EventStreamChangeEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

#endif
}

public class TestDomainEvent : DomainEvent
{
    public TestDomainEvent() : base("arootid")
    {
    }
}