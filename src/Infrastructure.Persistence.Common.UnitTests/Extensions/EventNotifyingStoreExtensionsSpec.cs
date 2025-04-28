using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
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
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([]));

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
        var change = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.Id)
            .Returns("anid".ToId());
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change]));

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
        var change = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.Id)
            .Returns("anid".ToId());
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change]));
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
        var change1 = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change2 = EventSourcedChangeEvent.Create("aneventid2".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change3 = EventSourcedChangeEvent.Create("aneventid3".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.Id)
            .Returns("anid".ToId());
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));

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

    [Fact]
    public async Task WhenSaveAndPublishEventsForSameAggregateRepeatedlyInParallel_ThenSequencesPublishing()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate = new Mock<IChangeEventProducingAggregateRoot>();
        var change1 = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change2 = EventSourcedChangeEvent.Create("aneventid2".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change3 = EventSourcedChangeEvent.Create("aneventid3".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        aggregate.Setup(a => a.Id)
            .Returns("anid".ToId());
        aggregate.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        var wasCalled = false;
        var calledCount = new Dictionary<ISingleValueObject<string>, int>
        {
            { "anid".ToId(), 0 }
        };
        var action = new Func<Task<Result<Error>>>(() => store.Object.SaveAndPublishChangesAsync(aggregate.Object,
            OnEventStreamChanged, async (root, _, token) =>
            {
                var rootId = root.Id;
                wasCalled = true;
                // Simple concurrency check for parallel processing
                calledCount[rootId] += 1;
                await Task.Delay(200, token); //wait and see if another thread calls the line before
                if (calledCount[rootId] > 1)
                {
                    calledCount[rootId] = 0;
                    return Error.EntityExists("Concurrency issue detected for aggregate: {0}".Format(rootId));
                }

                calledCount[rootId] = 0;
                return "aname";
            }, CancellationToken.None));

        var result = await Tasks.WhenAllAsync(action(), action(), action(), action(), action());

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        aggregate.Verify(a => a.GetChanges(), Times.Exactly(5));
        aggregate.Verify(a => a.ClearChanges(), Times.Exactly(5));
        _raisedEvents.Count.Should().Be(3);
        _raisedEvents[0].Id.Should().Be("aneventid1");
        _raisedEvents[1].Id.Should().Be("aneventid2");
        _raisedEvents[2].Id.Should().Be("aneventid3");
    }

    [Fact]
    public async Task WhenSaveAndPublishEventsForDifferentAggregatesInParallel_ThenParallelizesPublishing()
    {
        var store = new Mock<IEventNotifyingStore>();
        var aggregate1 = new Mock<IChangeEventProducingAggregateRoot>();
        var aggregate2 = new Mock<IChangeEventProducingAggregateRoot>();
        var aggregate3 = new Mock<IChangeEventProducingAggregateRoot>();
        var aggregate4 = new Mock<IChangeEventProducingAggregateRoot>();
        var aggregate5 = new Mock<IChangeEventProducingAggregateRoot>();
        var change1 = EventSourcedChangeEvent.Create("aneventid1".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change2 = EventSourcedChangeEvent.Create("aneventid2".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        var change3 = EventSourcedChangeEvent.Create("aneventid3".ToId(), "anentitytype", false, "aneventtype",
            "jsondata",
            "metadata", 1);
        aggregate1.Setup(a => a.Id)
            .Returns("anid1".ToId());
        aggregate1.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        aggregate2.Setup(a => a.Id)
            .Returns("anid2".ToId());
        aggregate2.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        aggregate3.Setup(a => a.Id)
            .Returns("anid3".ToId());
        aggregate3.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        aggregate4.Setup(a => a.Id)
            .Returns("anid4".ToId());
        aggregate4.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        aggregate5.Setup(a => a.Id)
            .Returns("anid5".ToId());
        aggregate5.Setup(a => a.GetChanges())
            .Returns(new Result<List<EventSourcedChangeEvent>, Error>([change1, change2, change3]));
        var wasCalled = false;
        var calledCount = new Dictionary<ISingleValueObject<string>, int>
        {
            { "anid1".ToId(), 0 },
            { "anid2".ToId(), 0 },
            { "anid3".ToId(), 0 },
            { "anid4".ToId(), 0 },
            { "anid5".ToId(), 0 }
        };
        var action = new Func<IChangeEventProducingAggregateRoot, Task<Result<Error>>>(root =>
            store.Object.SaveAndPublishChangesAsync(root,
                OnEventStreamChanged, async (_, _, token) =>
                {
                    var rootId = root.Id;
                    wasCalled = true;
                    // Simple concurrency check for parallel processing
                    calledCount[rootId] += 1;
                    await Task.Delay(200, token); //wait and see if another thread calls the line before
                    if (calledCount[rootId] > 1)
                    {
                        calledCount[rootId] = 0;
                        return Error.EntityExists("Concurrency issue detected for aggregate: {0}".Format(rootId));
                    }

                    calledCount[rootId] = 0;
                    return "aname";
                }, CancellationToken.None));

        var result = await Tasks.WhenAllAsync(action(aggregate1.Object), action(aggregate2.Object),
            action(aggregate3.Object), action(aggregate4.Object), action(aggregate5.Object));

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        aggregate1.Verify(a => a.GetChanges(), Times.Once());
        aggregate1.Verify(a => a.ClearChanges(), Times.Once());
        aggregate2.Verify(a => a.GetChanges(), Times.Once());
        aggregate2.Verify(a => a.ClearChanges(), Times.Once());
        aggregate3.Verify(a => a.GetChanges(), Times.Once());
        aggregate3.Verify(a => a.ClearChanges(), Times.Once());
        aggregate4.Verify(a => a.GetChanges(), Times.Once());
        aggregate4.Verify(a => a.ClearChanges(), Times.Once());
        aggregate5.Verify(a => a.GetChanges(), Times.Once());
        aggregate5.Verify(a => a.ClearChanges(), Times.Once());
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
            var tasks = new List<Task<Result<Error>>>
                { Task.FromResult<Result<Error>>(Error.RuleViolation("amessage")) };
            args.AddTasks(_ => tasks);
            return;
        }

        _raisedEvents = args.Events;
    }
}