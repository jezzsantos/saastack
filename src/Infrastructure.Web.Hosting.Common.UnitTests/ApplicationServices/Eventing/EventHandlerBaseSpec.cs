using Application.Persistence.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices.Eventing;

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
        _handler.OnEventStreamStateChanged(null,
            new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    EntityType = null!,
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
                    EntityType = null!,
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
                    EntityType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            }), CancellationToken.None);

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
    }

    [Fact]
    public void WhenEventStreamChangedEventRaisedAndFromDifferentStreamsAndWriteFails_ThenWritesRemainingBatches()
    {
        _action.Setup(rms =>
                rms(It.IsAny<string>(), It.IsAny<List<EventStreamChangeEvent>>()))
            .Throws<Exception>();

        _handler.OnEventStreamStateChanged(null,
            new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    EntityType = null!,
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
                    EntityType = null!,
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
                    EntityType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            }), CancellationToken.None);

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
    }

    [Fact]
    public void WhenEventStreamChangedEventRaisedAndEventsAreOutOfOrder_ThenCapturesErrors()
    {
        _handler.OnEventStreamStateChanged(null,
            new EventStreamChangedArgs(new List<EventStreamChangeEvent>
            {
                new()
                {
                    Id = "aneventid1",
                    StreamName = "astreamname1",
                    Version = 5,
                    Data = null!,
                    Metadata = null!,
                    EntityType = null!,
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
                    EntityType = null!,
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
                    EntityType = null!,
                    EventType = null!,
                    LastPersistedAtUtc = default
                }
            }), CancellationToken.None);

        _handler.ProcessingErrors.Should().HaveCount(1);
        _handler.ProcessingErrors[0].Exception.Should().BeOfType<InvalidOperationException>();
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