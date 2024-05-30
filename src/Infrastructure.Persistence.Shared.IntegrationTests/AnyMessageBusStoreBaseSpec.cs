using Common;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public abstract class AnyMessageBusStoreBaseSpec
{
    protected AnyMessageBusStoreBaseSpec(IMessageBusStore messageBusStore,
        string subscriptionName = "asubscriptionname")
    {
        Info = new MessageBusStoreInfo
        {
            Store = messageBusStore,
            TopicName = typeof(TestDataStoreEntity).GetEntityNameSafe(),
            SubscriptionName = subscriptionName
        };
#if TESTINGONLY
        Info.Store.DestroyAllAsync(Info.TopicName, CancellationToken.None).GetAwaiter()
            .GetResult();
#endif
        Info.Store.SubscribeAsync(Info.TopicName, Info.SubscriptionName, CancellationToken.None).GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task WhenReceiveSingleWithNullTopicName_ThenThrows()
    {
#if TESTINGONLY
        await Info.Store
            .Invoking(x => x.ReceiveSingleAsync(null!, Info.SubscriptionName, (_, _) => Task.FromResult(Result.Ok),
                CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleWithNullSubscriptionName_ThenThrows()
    {
#if TESTINGONLY
        await Info.Store
            .Invoking(x => x.ReceiveSingleAsync(Info.TopicName, null!, (_, _) => Task.FromResult(Result.Ok),
                CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleWithNullHandler_ThenThrows()
    {
#if TESTINGONLY
        await Info.Store
            .Invoking(x =>
                x.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName, null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleAndNoMessage_ThenReturnsFalse()
    {
        var wasCalled = false;
#if TESTINGONLY
        var result = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (_, _) =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result.Value.Should().BeFalse();
#endif
        wasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReceiveSingleAndMessageExists_ThenExecutesHandler()
    {
        await Info.Store.SendAsync(Info.TopicName, "amessage", CancellationToken.None);

#if TESTINGONLY
        string? message = null;
        var result = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result.Value.Should().BeTrue();
        message.Should().Be("amessage");

        var count = await Info.Store.CountAsync(Info.TopicName, Info.SubscriptionName, CancellationToken.None);
        count.Value.Should().Be(0);
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleAndMessageExistsAndHandlerReturnsError_ThenLeavesMessageInSubscription()
    {
        await Info.Store.SendAsync(Info.TopicName, "amessage", CancellationToken.None);

#if TESTINGONLY
        var result1 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (_, _) => Task.FromResult<Result<Error>>(Error.RuleViolation("amessage")),
            CancellationToken.None);

        result1.Should().BeError(ErrorCode.RuleViolation, "amessage");

        var count = await Info.Store.CountAsync(Info.TopicName, Info.SubscriptionName, CancellationToken.None);
        count.Value.Should().Be(1);

        string? remainingMessage = null;
        var result2 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (msg, _) =>
            {
                remainingMessage = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result2.Value.Should().BeTrue();
        remainingMessage.Should().Be("amessage");
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleAndMessageExistsAndHandlerThrows_ThenLeavesMessageInSubscription()
    {
        await Info.Store.SendAsync(Info.TopicName, "amessage", CancellationToken.None);

#if TESTINGONLY
        var result1 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (_, _) => throw new Exception("amessage"),
            CancellationToken.None);

        result1.Should().BeError(ErrorCode.Unexpected, "amessage");

        var count = await Info.Store.CountAsync(Info.TopicName, Info.SubscriptionName,
            CancellationToken.None);
        count.Value.Should().Be(1);

        string? remainingMessage = null;
        var result2 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (msg, _) =>
            {
                remainingMessage = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result2.Value.Should().BeTrue();
        remainingMessage.Should().Be("amessage");
#endif
    }

    [Fact]
    public virtual async Task WhenReceiveSingleOnTwoSubscriptions_ThenReturnsSameMessage()
    {
        Info.Store.SubscribeAsync(Info.TopicName, "asubscriptionname1", CancellationToken.None).GetAwaiter()
            .GetResult();
        Info.Store.SubscribeAsync(Info.TopicName, "asubscriptionname2", CancellationToken.None).GetAwaiter()
            .GetResult();

        await Info.Store.SendAsync(Info.TopicName, "amessage", CancellationToken.None);

#if TESTINGONLY
        string? message = null;
        var result1 = await Info.Store.ReceiveSingleAsync(Info.TopicName, "asubscriptionname1",
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result1.Value.Should().BeTrue();
        message.Should().Be("amessage");

        message = null;
        var result2 = await Info.Store.ReceiveSingleAsync(Info.TopicName, "asubscriptionname2",
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result2.Value.Should().BeTrue();
        message.Should().Be("amessage");

        var count1 = await Info.Store.CountAsync(Info.TopicName, "asubscriptionname1", CancellationToken.None);
        count1.Value.Should().Be(0);
        var count2 = await Info.Store.CountAsync(Info.TopicName, "asubscriptionname2", CancellationToken.None);
        count2.Value.Should().Be(0);
#endif
    }

    [Fact]
    public async Task WhenReceiveSingleAgainOnLastMessage_ThenReturnsFalse()
    {
        await Info.Store.SendAsync(Info.TopicName, "amessage", CancellationToken.None);

#if TESTINGONLY
        string? message = null;
        var result1 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result1.Value.Should().BeTrue();
        message.Should().Be("amessage");

        message = null;
        var result2 = await Info.Store.ReceiveSingleAsync(Info.TopicName, Info.SubscriptionName,
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result2.Value.Should().BeFalse();
        message.Should().BeNull();

        var count = await Info.Store.CountAsync(Info.TopicName, Info.SubscriptionName, CancellationToken.None);
        count.Value.Should().Be(0);
#endif
    }

    protected MessageBusStoreInfo Info { get; }

    public class MessageBusStoreInfo
    {
        public required IMessageBusStore Store { get; set; }

        public required string SubscriptionName { get; set; }

        public required string TopicName { get; set; }
    }
}