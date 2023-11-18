using Common;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Xunit;

namespace Infrastructure.Persistence.Common.IntegrationTests;

public abstract class AnyQueueStoreBaseSpec
{
    private readonly QueueStoreInfo _setup;

    protected AnyQueueStoreBaseSpec(IQueueStore queueStore)
    {
        _setup = new QueueStoreInfo
            { Store = queueStore, QueueName = typeof(TestDataStoreEntity).GetEntityNameSafe() };
        _setup.Store.DestroyAllAsync(_setup.QueueName, CancellationToken.None).GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task WhenPopSingleWithNullQueueName_ThenThrows()
    {
        await _setup.Store
            .Invoking(x => x.PopSingleAsync(null!, (_, _) => Task.FromResult(Result.Ok), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenPopSingleWithNullHandler_ThenThrows()
    {
        await _setup.Store
            .Invoking(x => x.PopSingleAsync(_setup.QueueName, null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenPopSingleAndNoMessage_ThenDoesNothing()
    {
        var wasCalled = false;
        var result = await _setup.Store.PopSingleAsync(_setup.QueueName,
            (_, _) =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result.Value.Should().BeFalse();
        wasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task WhenPopSingleAndMessageExists_ThenExecutesHandler()
    {
        await _setup.Store.PushAsync(_setup.QueueName, "amessage", CancellationToken.None);

        string? message = null;
        var result = await _setup.Store.PopSingleAsync(_setup.QueueName,
            (msg, _) =>
            {
                message = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result.Value.Should().BeTrue();
        message.Should().Be("amessage");

        var count = await _setup.Store.CountAsync(_setup.QueueName, CancellationToken.None);
        count.Value.Should().Be(0);
    }

    [Fact]
    public async Task WhenPopSingleAndMessageExistsAndHandlerFails_ThenLeavesMessageOnQueue()
    {
        await _setup.Store.PushAsync(_setup.QueueName, "amessage", CancellationToken.None);

        await _setup.Store.PopSingleAsync(_setup.QueueName, (_, _) => throw new Exception(),
            CancellationToken.None);

        var count = await _setup.Store.CountAsync(_setup.QueueName,
            CancellationToken.None);
        count.Value.Should().Be(1);

        string? remainingMessage = null;
        var result = await _setup.Store.PopSingleAsync(_setup.QueueName,
            (msg, _) =>
            {
                remainingMessage = msg;
                return Task.FromResult(Result.Ok);
            }, CancellationToken.None);

        result.Value.Should().BeTrue();
        remainingMessage.Should().Be("amessage");
    }

    public class QueueStoreInfo
    {
        public required string QueueName { get; set; }

        public required IQueueStore Store { get; set; }
    }
}