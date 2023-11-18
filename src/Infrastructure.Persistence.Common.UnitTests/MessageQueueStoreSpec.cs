using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class MessageQueueStoreSpec
{
    private readonly Mock<IQueueStore> _queueStore;
    private readonly MessageQueueStore<TestQueuedMessage> _store;

    public MessageQueueStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        _queueStore = new Mock<IQueueStore>();
        _store = new MessageQueueStore<TestQueuedMessage>(recorder.Object, _queueStore.Object);
    }

    [Fact]
    public async Task WhenCount_ThenGetsCountFromStore()
    {
        await _store.CountAsync(CancellationToken.None);

        _queueStore.Verify(repo => repo.CountAsync("aqueuename", CancellationToken.None));
    }

    [Fact]
    public async Task WhenDestroyAll_ThenDestroysAllInStore()
    {
        await _store.DestroyAllAsync(CancellationToken.None);

        _queueStore.Verify(repo => repo.DestroyAllAsync("aqueuename", CancellationToken.None));
    }

    [Fact]
    public async Task WhenPopSingleAndNotExists_ThenDoesNotHandle()
    {
        _queueStore.Setup(repo =>
                repo.PopSingleAsync("aqueuename", It.IsAny<Func<string, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((string _, Func<string, CancellationToken, Task<Result<Error>>> _, CancellationToken _) => { })
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var wasCalled = false;
        var result = await _store.PopSingleAsync((_, _) =>
        {
            wasCalled = true;
            return Task.FromResult(Result.Ok);
        }, CancellationToken.None);

        result.Value.Should().BeFalse();
        wasCalled.Should().BeFalse();
        _queueStore.Verify(
            repo => repo.PopSingleAsync("aqueuename", It.IsAny<Func<string, CancellationToken, Task<Result<Error>>>>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenPopSingleAndExists_ThenHandles()
    {
        var message = new TestQueuedMessage();
        _queueStore.Setup(repo =>
                repo.PopSingleAsync("aqueuename", It.IsAny<Func<string, CancellationToken, Task<Result<Error>>>>(),
                    It.IsAny<CancellationToken>()))
            .Callback((string _, Func<string, CancellationToken, Task<Result<Error>>> action, CancellationToken _) =>
            {
                action(message.ToJson()!, CancellationToken.None);
            })
            .Returns(Task.FromResult<Result<bool, Error>>(true));

        var wasCalled = false;
        IQueuedMessage? calledMessage = null;
        var result = await _store.PopSingleAsync((msg, _) =>
        {
            wasCalled = true;
            calledMessage = msg;
            return Task.FromResult(Result.Ok);
        }, CancellationToken.None);

        result.Value.Should().BeTrue();
        wasCalled.Should().BeTrue();
        calledMessage.Should().BeEquivalentTo(message);
        _queueStore.Verify(
            repo => repo.PopSingleAsync("aqueuename", It.IsAny<Func<string, CancellationToken, Task<Result<Error>>>>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenPush_ThenPushesToStore()
    {
        var message = new TestQueuedMessage
        {
            ABooleanValue = true,
            ADoubleValue = 9,
            AStringProperty = "avalue"
        };
        var call = Mock.Of<ICallContext>(call => call.CallId == "acallid" && call.CallerId == "acallerid");
        await _store.PushAsync(call, message, CancellationToken.None);

        _queueStore.Verify(repo => repo.PushAsync("aqueuename", It.Is<string>(json =>
            json.FromJson<TestQueuedMessage>()!.MessageId.HasValue()
            && json.FromJson<TestQueuedMessage>()!.CallId == "acallid"
            && json.FromJson<TestQueuedMessage>()!.CallerId == "acallerid"
            && json.FromJson<TestQueuedMessage>()!.AStringProperty == "avalue"
        ), CancellationToken.None));
    }
}