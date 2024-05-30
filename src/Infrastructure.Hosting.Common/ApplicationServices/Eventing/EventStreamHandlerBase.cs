using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;

namespace Infrastructure.Hosting.Common.ApplicationServices.Eventing;

/// <summary>
///     Provides a base handler that subscribes to one or more <see cref="IEventNotifyingStore" />
///     instances, listens to them raise change events, and relays them to derived class.
/// </summary>
public abstract class EventStreamHandlerBase : IDisposable
{
    private readonly IEventNotifyingStore[] _eventingStores;
    private readonly IRecorder _recorder;

    protected EventStreamHandlerBase(IRecorder recorder, params IEventNotifyingStore[] eventingStores)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _eventingStores = eventingStores;
    }

    public Guid InstanceId { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Stop();
    }

    public IReadOnlyList<IEventNotifyingStore> EventingStores => _eventingStores;

    public bool IsStarted { get; private set; }

    public virtual void Start()
    {
        if (!IsStarted)
        {
            foreach (var storage in _eventingStores)
            {
                storage.OnEventStreamChanged += OnEventStreamStateChanged;
                _recorder.TraceDebug(null, "Subscribed {Instance} to events for {Storage}", InstanceId,
                    storage.GetType().ToString());
            }

            IsStarted = true;
        }
    }

    public virtual void Stop()
    {
        if (IsStarted)
        {
            foreach (var storage in _eventingStores)
            {
                storage.OnEventStreamChanged -= OnEventStreamStateChanged;
                _recorder.TraceDebug(null, "Unsubscribed {Instance} from events for {Storage}", InstanceId,
                    storage.GetType().ToString());
            }

            IsStarted = false;
        }
    }

    protected abstract Task<Result<Error>> HandleStreamEventsAsync(string streamName,
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken);

    protected internal void OnEventStreamStateChanged(object? sender, EventStreamChangedArgs args,
        CancellationToken cancellationToken)
    {
        if (args.Events.HasNone())
        {
            return;
        }

        args.AddTasks(events =>
        {
            var eventsStreams = events.GroupBy(e => e.StreamName)
                .Select(grp => grp.AsEnumerable())
                .Select(grp => grp.OrderBy(e => e.Version).ToList())
                .ToList();

            var tasks = new List<Task<Result<Error>>>();
            foreach (var eventStream in eventsStreams)
            {
                var firstEvent = Enumerable.First(eventStream);
                var streamName = firstEvent.StreamName;

                var ensured = EnsureContiguousVersions(streamName, eventStream);
                if (ensured.IsFailure)
                {
                    tasks.Add(Task.FromResult<Result<Error>>(ensured.Error));
                    continue;
                }

                try
                {
                    var handleTask = HandleStreamEventsAsync(streamName, eventStream, CancellationToken.None);
                    tasks.Add(handleTask);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        Resources.EventStreamHandlerBase_OnEventStreamStateChanged_FailedToHandle.Format(streamName),
                        ex);
                }

            }

            return tasks;
        });
    }

    private static Result<Error> EnsureContiguousVersions(string streamName, List<EventStreamChangeEvent> eventStream)
    {
        if (!eventStream.HasContiguousVersions())
        {
            return Error.RuleViolation(Resources.EventStreamHandlerBase_OutOfOrderEvents.Format(streamName));
        }

        return Result.Ok;
    }
}

internal static class EventStreamExtensions
{
    public static bool HasContiguousVersions(this List<EventStreamChangeEvent> events)
    {
        if (!events.Any())
        {
            return true;
        }

        static IEnumerable<int> GetRange(int start, int count)
        {
            for (var next = 0; next < count; next++)
            {
                yield return start + next;
            }
        }

        var expectedRange = GetRange(Enumerable.First(events).Version, events.Count);
        return events.Select(e => e.Version).SequenceEqual(expectedRange);
    }
}