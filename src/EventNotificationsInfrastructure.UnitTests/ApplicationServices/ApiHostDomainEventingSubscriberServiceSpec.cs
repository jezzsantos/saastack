using Application.Interfaces;
using Common;
using Common.Configuration;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostDomainEventingSubscriberServiceSpec
{
    private readonly ApiHostDomainEventingSubscriberService _service;
    private readonly Mock<IMessageBusStore> _store;

    public ApiHostDomainEventingSubscriberServiceSpec()
    {
        var recorder = new Mock<IRecorder>();
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s =>
                s.Platform.GetString(ApiHostDomainEventingSubscriberService.SubscriptionNameSettingName,
                    It.IsAny<string>()))
            .Returns("ahostname");
        _store = new Mock<IMessageBusStore>();

        _service =
            new ApiHostDomainEventingSubscriberService(recorder.Object, settings.Object, _store.Object,
                [typeof(TestConsumer1), typeof(TestConsumer2)]);
    }

    [Fact]
    public void WhenConstructed_ThenHasConsumers()
    {
        var result = _service.Consumers;

        result.Count.Should().Be(2);
        result[typeof(TestConsumer1)].Should()
            .Be("ahostname-Event-UnitTests-ApplicationServices-Test");
        result[typeof(TestConsumer2)].Should()
            .Be("ahostname-Event-UnitTests-ApplicationServices-Test");
    }

    [Fact]
    public async Task WhenRegisterAllSubscribersAsync_ThenRegistersAll()
    {
        var result = await _service.RegisterAllSubscribersAsync(CancellationToken.None);

        result.Should().BeSuccess();
        _store.Verify(store => store.SubscribeAsync(EventingConstants.Topics.DomainEvents,
            "ahostname-Event-UnitTests-ApplicationServices-Test",
            It.IsAny<CancellationToken>()));
    }
}

public class TestConsumer1 : IDomainEventNotificationConsumer
{
    public Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
}

public class TestConsumer2 : IDomainEventNotificationConsumer
{
    public Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
}