using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Persistence.Common.Extensions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class EventNotifyingStoreExtensionsSpec : IEventNotifyingStore
{
    private bool _failHandler;
    private IReadOnlyList<EventStreamChangeEvent> _raisedEvents;

    public EventNotifyingStoreExtensionsSpec()
    {
        _failHandler = false;
        _raisedEvents = new List<EventStreamChangeEvent>();
        OnEventStreamChanged += OnOnEventStreamChanged;
    }

    [Fact]
    public async Task WhenSaveAndPublishEventsAsyncAndAggregateHasNoChanges_ThenReturns()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate = new Mock<IChangeEventProducingAggregateRoot>();
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>(new List<EventSourcedChangeEvent>()));

        var wasCalled = false;
        var result = await store.Object.SaveAndPublishChangesAsync(aggregate.Object,
            OnEventStreamChanged, (_, _, _) =>
            {
                wasCalled = true;
                return Task.FromResult<Result<string, Error>>("aname");
            }, CancellationToken.None);

        result.Should().BeSuccess();
        wasCalled.Should().BeFalse();
        aggregate.Verify(a => a.GetChanges());
        aggregate.Verify(a => a.ClearChanges(), Times.Never);
    }

    [Fact]
    public async Task WhenSaveAndPublishEventsAsyncAndAggregateHasChangesButSaveFails_ThenReturnsError()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate = new Mock<IChangeEventProducingAggregateRoot>();
        var change = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", "aneventtype", "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>(new List<EventSourcedChangeEvent>
            {
                change
            }));

        var wasCalled = false;
        var result = await store.Object.SaveAndPublishChangesAsync(aggregate.Object,
            OnEventStreamChanged, (_, _, _) =>
            {
                wasCalled = true;
                return Task.FromResult<Result<string, Error>>(Error.RuleViolation("amessage"));
            }, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, "amessage");
        wasCalled.Should().BeTrue();
        aggregate.Verify(a => a.GetChanges());
        aggregate.Verify(a => a.ClearChanges(), Times.Never);
        _raisedEvents.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenSaveAndPublishEventsAsyncAndAggregateHasChangesButPublishFails_ThenReturnsError()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate = new Mock<IChangeEventProducingAggregateRoot>();
        var change = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", "aneventtype", "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>(new List<EventSourcedChangeEvent>
            {
                change
            }));
        _failHandler = true;

        var wasCalled = false;
        var result = await store.Object.SaveAndPublishChangesAsync(aggregate.Object,
            OnEventStreamChanged, (_, _, _) =>
            {
                wasCalled = true;
                return Task.FromResult<Result<string, Error>>("aname");
            }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected,
            Error.RuleViolation(
                    $"{Resources.EventSourcingDddCommandStore_PublishFailed.Format("aname")}{Environment.NewLine}\tamessage")
                .ToString());
        wasCalled.Should().BeTrue();
        aggregate.Verify(a => a.GetChanges());
        aggregate.Verify(a => a.ClearChanges());
        _raisedEvents.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenSaveAndPublishEventsAsyncAndAggregateHasChanges_ThenSavesAndPublishes()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate = new Mock<IChangeEventProducingAggregateRoot>();
        var change1 = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", "aneventtype", "jsondata",
            "metadata", 1);
        var change2 = EventSourcedChangeEvent.Create("aneventid2".ToId(), "anentitytype", "aneventtype", "jsondata",
            "metadata", 1);
        var change3 = EventSourcedChangeEvent.Create("aneventid3".ToId(), "anentitytype", "aneventtype", "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>(new List<EventSourcedChangeEvent>
            {
                change1, change2, change3
            }));

        var wasCalled = false;
        var result = await store.Object.SaveAndPublishChangesAsync(aggregate.Object,
            OnEventStreamChanged, (_, _, _) =>
            {
                wasCalled = true;
                return Task.FromResult<Result<string, Error>>("aname");
            }, CancellationToken.None);

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        aggregate.Verify(a => a.GetChanges());
        aggregate.Verify(a => a.ClearChanges());
        _raisedEvents.Count.Should().Be(3);
        _raisedEvents[0].Id.Should().Be("aneventid1");
        _raisedEvents[1].Id.Should().Be("aneventid2");
        _raisedEvents[2].Id.Should().Be("aneventid3");
    }

    public event EventStreamChangedAsync<EventStreamChangedArgs>? OnEventStreamChanged;

    private void OnOnEventStreamChanged(object sender, EventStreamChangedArgs args, CancellationToken cancellationToken)
    {
        if (_failHandler)
        {
            args.CreateTasksAsync(_ => Task.FromResult<Result<Error>>(Error.RuleViolation("amessage")));
            return;
        }

        _raisedEvents = args.Events;
    }
}