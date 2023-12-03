using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing;

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
        _recorder = recorder;
        _eventingStores = eventingStores;
        ProcessingErrors = new List<EventProcessingError>();
    }

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

        if (IsStarted)
        {
            foreach (var storage in _eventingStores)
            {
                storage.OnEventStreamChanged -= OnEventStreamStateChanged;
            }
        }
    }

    public IReadOnlyList<IEventNotifyingStore> EventingStores => _eventingStores;

    public bool IsStarted { get; private set; }

    internal List<EventProcessingError> ProcessingErrors { get; }

    public void Start()
    {
        if (!IsStarted)
        {
            foreach (var storage in _eventingStores)
            {
                storage.OnEventStreamChanged += OnEventStreamStateChanged;
                _recorder.TraceDebug(null, "Subscribed to events for {Storage}", storage.GetType().Name);
            }

            IsStarted = true;
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

        args.CreateTasksAsync(async events =>
        {
            return await WithProcessMonitoringAsync(async () =>
            {
                var eventsStreams = events.GroupBy(e => e.StreamName)
                    .Select(grp => grp.AsEnumerable())
                    .Select(grp => grp.OrderBy(e => e.Version).ToList());

                foreach (var eventStream in eventsStreams)
                {
                    var firstEvent = Enumerable.First(eventStream);
                    var streamName = firstEvent.StreamName;

                    try
                    {
                        var ensured = EnsureContiguousVersions(streamName, eventStream);
                        if (!ensured.IsSuccessful)
                        {
                            ProcessingErrors.Add(
                                new EventProcessingError(ensured.Error, streamName));
                        }

                        var handled = await HandleStreamEventsAsync(streamName, eventStream, CancellationToken.None);
                        if (!handled.IsSuccessful)
                        {
                            ProcessingErrors.Add(
                                new EventProcessingError(handled.Error, streamName));
                        }
                    }
                    catch (Exception ex)
                    {
                        ProcessingErrors.Add(
                            new EventProcessingError(ex, streamName));

                        //Continue onto next stream
                    }
                }

                return Result.Ok;
            });
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

    private Task<Result<Error>> WithProcessMonitoringAsync(Func<Task<Result<Error>>> process)
    {
        ProcessingErrors.Clear();

        var result = process.Invoke();
        if (ProcessingErrors.Any())
        {
            ProcessingErrors.ForEach(error =>
                _recorder.TraceError(null, error.Exception,
                    "Failed to relay new events to read model for: {StreamName}", error.StreamName));
        }

        return result;
    }

    internal class EventProcessingError
    {
        public EventProcessingError(Exception ex, string streamName)
        {
            Exception = ex;
            StreamName = streamName;
        }

        public EventProcessingError(Error error, string streamName)
        {
            Exception = new InvalidOperationException(error.ToString());
            StreamName = streamName;
        }

        public Exception Exception { get; }

        public string StreamName { get; }
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