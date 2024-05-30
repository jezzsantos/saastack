using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing;

[UsedImplicitly]
public sealed class EventHandlerBaseSpec
{
    [Trait("Category", "Unit")]
    public sealed class GivenANonAwaitableHandler : IDisposable
    {
        private readonly Mock<Action<string, List<EventStreamChangeEvent>>> _action;
        private readonly TestEventHandler _handler;

        public GivenANonAwaitableHandler()
        {
            _action = new Mock<Action<string, List<EventStreamChangeEvent>>>();
            _handler = new TestEventHandler(_action);
        }

        ~GivenANonAwaitableHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handler.Dispose();
            }
        }

        [Fact]
        public void WhenEventStreamChangedEventRaisedAndNoEvents_ThenDoesNotWriteEvents()
        {
            _handler.OnEventStreamStateChanged(null, new EventStreamChangedArgs(new List<EventStreamChangeEvent>()),
                CancellationToken.None);

            _action.Verify(
                act => act("astreamname", It.IsAny<List<EventStreamChangeEvent>>()), Times.Never);
        }

        [Fact]
        public async Task WhenEventStreamChangedEventRaisedAndFromDifferentStreams_ThenWritesBatchedEvents()
        {
            var args = new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                },
                new()
                {
                    Id = "aneventid2",
                    StreamName = "astreamname2",
                    Version = 3,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                },
                new()
                {
                    Id = "aneventid3",
                    StreamName = "astreamname1",
                    Version = 4,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            });
            _handler.OnEventStreamStateChanged(null, args, CancellationToken.None);

            await args.CompleteAsync();

            _action.Verify(
                act => act("astreamname1", It.Is<List<EventStreamChangeEvent>>(batch =>
                    batch.Count == 2
                    && batch[0].Id == "aneventid3"
                    && batch[1].Id == "aneventid1"
                )), Times.Once);
            _action.Verify(
                act => act("astreamname2", It.Is<List<EventStreamChangeEvent>>(batch =>
                    batch.Count == 1
                    && batch[0].Id == "aneventid2"
                )), Times.Once);
            args.Tasks.Count.Should().Be(2);
        }

        [Fact]
        public void WhenEventStreamChangedEventRaisedAndWriteThrows_ThenThrows()
        {
            var exception = new Exception("amessage");
            _action.Setup(act => act(It.IsAny<string>(), It.IsAny<List<EventStreamChangeEvent>>()))
                .Throws(exception);

            var args = new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            });

            _handler.Invoking(x => x.OnEventStreamStateChanged(null,
                    args, CancellationToken.None))
                .Should().Throw<Exception>()
                .WithMessage(
                    Resources.EventStreamHandlerBase_OnEventStreamStateChanged_FailedToHandle.Format("astreamname1"))
                .WithInnerException<Exception>().WithMessage("amessage");

            _action.Verify(
                act => act("astreamname1", It.Is<List<EventStreamChangeEvent>>(batch =>
                    batch.Count == 1
                    && batch[0].Id == "aneventid1"
                )), Times.Once);
            args.Tasks.Count.Should().Be(0);
        }

        [Fact]
        public void WhenEventStreamChangedEventRaisedAndEventsAreOutOfOrder_ThenReturnsError()
        {
            var args = new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                },
                new()
                {
                    Id = "aneventid2",
                    StreamName = "astreamname1",
                    Version = 2,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                },
                new()
                {
                    Id = "aneventid3",
                    StreamName = "astreamname1",
                    Version = 4,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            });
            _handler.OnEventStreamStateChanged(null,
                args, CancellationToken.None);

            args.Tasks.Count.Should().Be(1);
            args.Tasks[0].Result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EventStreamHandlerBase_OutOfOrderEvents.Format("astreamname1"));
        }
    }

    [Trait("Category", "Unit")]
    public sealed class GivenAnAwaitableHandler : IDisposable
    {
        private readonly Mock<Func<string, List<EventStreamChangeEvent>, Task>> _action;
        private readonly TestAwaitedEventHandler _handler;

        public GivenAnAwaitableHandler()
        {
            _action = new Mock<Func<string, List<EventStreamChangeEvent>, Task>>();
            _handler = new TestAwaitedEventHandler(_action);
        }

        ~GivenAnAwaitableHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handler.Dispose();
            }
        }

        [Fact]
        public void WhenEventStreamChangedEventRaisedAndWriteThrows_ThenThrows()
        {
            var exception = new Exception("amessage");
            _action.Setup(act => act(It.IsAny<string>(), It.IsAny<List<EventStreamChangeEvent>>()))
                .Returns(Task.FromException(exception));

            var args = new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    RootAggregateType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            });

            _handler.OnEventStreamStateChanged(null,
                args, CancellationToken.None);

            args.Invoking(x => x.CompleteAsync())
                .Should()
                .ThrowAsync<Exception>()
                .WithMessage("amessage");

            _action.Verify(
                act => act("astreamname1", It.Is<List<EventStreamChangeEvent>>(batch =>
                    batch.Count == 1
                    && batch[0].Id == "aneventid1"
                )), Times.Once);
            args.Tasks.Count.Should().Be(1);
        }
    }
}

public class TestEventHandler : EventStreamHandlerBase
{
    private readonly Mock<Action<string, List<EventStreamChangeEvent>>> _action;

    public TestEventHandler(Mock<Action<string, List<EventStreamChangeEvent>>> action) : base(
        Mock.Of<IRecorder>(), Mock.Of<IEventSourcingDddCommandStore<TestEventingAggregateRoot>>())
    {
        _action = action;
    }

    protected override Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        _action.Object(streamName, eventStream);

        return Task.FromResult(Result.Ok);
    }
}

public class TestAwaitedEventHandler : EventStreamHandlerBase
{
    private readonly Mock<Func<string, List<EventStreamChangeEvent>, Task>> _action;

    public TestAwaitedEventHandler(Mock<Func<string, List<EventStreamChangeEvent>, Task>> action) : base(
        Mock.Of<IRecorder>(), Mock.Of<IEventSourcingDddCommandStore<TestEventingAggregateRoot>>())
    {
        _action = action;
    }

    protected override async Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        var task = _action.Object(streamName, eventStream);
        await task;

        return Result.Ok;
    }
}