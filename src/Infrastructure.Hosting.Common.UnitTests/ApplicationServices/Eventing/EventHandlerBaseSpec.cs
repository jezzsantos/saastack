using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing;

[Trait("Category", "Unit")]
public sealed class EventHandlerBaseSpec : IDisposable
{
    private readonly Mock<Action<string, List<EventStreamChangeEvent>>> _action;
    private readonly TestEventHandler _handler;

    public EventHandlerBaseSpec()
    {
        _action = new Mock<Action<string, List<EventStreamChangeEvent>>>();
        _handler = new TestEventHandler(_action);
    }

    ~EventHandlerBaseSpec()
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
            rms => rms("astreamname", It.IsAny<List<EventStreamChangeEvent>>()), Times.Never);
    }

    [Fact]
    public void WhenEventStreamChangedEventRaisedAndFromDifferentStreams_ThenWritesBatchedEvents()
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

        _action.Verify(
            rms => rms("astreamname1", It.Is<List<EventStreamChangeEvent>>(batch =>
                batch.Count == 2
                && batch[0].Id == "aneventid3"
                && batch[1].Id == "aneventid1"
            )), Times.Once);
        _action.Verify(
            rms => rms("astreamname2", It.Is<List<EventStreamChangeEvent>>(batch =>
                batch.Count == 1
                && batch[0].Id == "aneventid2"
            )), Times.Once);
        args.Tasks.Count.Should().Be(1);
    }

    [Fact]
    public void WhenEventStreamChangedEventRaisedAndFromDifferentStreamsAndWriteFails_ThenThrows()
    {
        var exception = new Exception("amessage");
        _action.Setup(rms => rms(It.IsAny<string>(), It.IsAny<List<EventStreamChangeEvent>>()))
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
        _handler.OnEventStreamStateChanged(null,
            args, CancellationToken.None);

        _action.Verify(
            rms => rms("astreamname1", It.Is<List<EventStreamChangeEvent>>(batch =>
                batch.Count == 2
                && batch[0].Id == "aneventid3"
                && batch[1].Id == "aneventid1"
            )), Times.Once);
        _action.Verify(rms => rms("astreamname2", It.IsAny<List<EventStreamChangeEvent>>()), Times.Never);
        args.Tasks.Count.Should().Be(1);
        args.Tasks[0].Invoking(x => x.Result)
            .Should().Throw<Exception>()
            .WithMessage(
                Resources.EventStreamHandlerBase_OnEventStreamStateChanged_FailedToProject.Format("astreamname1"))
            .WithInnerException<Exception>().WithMessage("amessage");
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

public class TestEventHandler : EventStreamHandlerBase
{
    private readonly Mock<Action<string, List<EventStreamChangeEvent>>> _mock;

    public TestEventHandler(Mock<Action<string, List<EventStreamChangeEvent>>> mock) : base(
        Mock.Of<IRecorder>(),
        Mock.Of<IEventSourcingDddCommandStore<TestEventingAggregateRoot>>())
    {
        _mock = mock;
    }

    protected override Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        _mock.Object(streamName, eventStream);
        return Task.FromResult(Result.Ok);
    }
}