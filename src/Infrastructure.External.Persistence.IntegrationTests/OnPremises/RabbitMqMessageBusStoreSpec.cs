using Common;
using FluentAssertions;
using Infrastructure.External.Persistence.OnPremises;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.OnPremises;

[Trait("Category", "Integration.Persistence")]
[Collection("RabbitMqMessageBus")]
public class RabbitMqMessageBusStoreSpec : AnyMessageBusStoreBaseSpec
{
    private readonly RabbitMqMessageBusSpecSetup _setup;

    public RabbitMqMessageBusStoreSpec(RabbitMqMessageBusSpecSetup setup) : base(setup.MessageBusStore)
    {
        _setup = setup;
    }

    [Fact]
    public async Task WhenSendWithInvalidTopicName_ThenThrows()
    {
        await _setup.MessageBusStore
            .Invoking(x => x.SendAsync("^aninvalidtopicname^", "amessage", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusTopicName);
    }

    [Fact]
    public async Task WhenReceiveSingleWithInvalidTopicName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.MessageBusStore
            .Invoking(x =>
                x.ReceiveSingleAsync("^aninvalidtopicname^", "asubscriptionname", (_, _) => Task.FromResult(Result.Ok),
                    CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusTopicName);
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleWithInvalidSubscriptionName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.MessageBusStore
            .Invoking(x =>
                x.ReceiveSingleAsync("atopicname", "^asubscriptionname^", (_, _) => Task.FromResult(Result.Ok),
                    CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusSubscriptionName);
#endif
    }

    [Fact]
    public async Task WhenCountWithInvalidTopicName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.MessageBusStore
            .Invoking(x => x.CountAsync("^aninvalidtopicname^", "asubscriptionname", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusTopicName);
#endif
    }

    [Fact]
    public async Task WhenCountWithInvalidSubscriptionName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.MessageBusStore
            .Invoking(x => x.CountAsync("atopicname", "^aninvalidsubscriptionname^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusSubscriptionName);
#endif
    }

    [Fact]
    public async Task WhenDestroyAllWithInvalidTopicName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.MessageBusStore
            .Invoking(x => x.DestroyAllAsync("^aninvalidtopicname^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidMessageBusTopicName);
#endif
    }
}