using Common;
using Task = Common.Extensions.Task;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store that can be notified when an event stream changes
/// </summary>
public interface IEventNotifyingStore
{
    /// <summary>
    ///     Fired when one or more events is added to an event stream
    /// </summary>
    event EventStreamChangedAsync<EventStreamChangedArgs> OnEventStreamChanged;
}

/// <summary>
///     Defines a delegate for handling changes to an event stream
/// </summary>
public delegate void EventStreamChangedAsync<in TArgs>(object sender, TArgs args, CancellationToken cancellationToken);

/// <summary>
///     Defines the arguments for the <see cref="EventStreamChangedAsync" /> delegate
/// </summary>
public sealed class EventStreamChangedArgs : EventArgs
{
    private readonly List<Task<Result<Error>>> _tasks = new();

    public EventStreamChangedArgs(IReadOnlyList<EventStreamChangeEvent> events)
    {
        Events = events;
    }

    /// <summary>
    ///     Returns the events
    /// </summary>
    public IReadOnlyList<EventStreamChangeEvent> Events { get; }

    /// <summary>
    ///     Completes all the tasks
    /// </summary>
    public async Task<Result<Error>> CompleteAsync()
    {
        return await Task.WhenAllAsync(_tasks.ToArray());
    }

    public IReadOnlyList<Task<Result<Error>>> Tasks => _tasks;

    /// <summary>
    ///     Creates a list of tasks to perform
    /// </summary>
    public void CreateTasksAsync(Func<IReadOnlyList<EventStreamChangeEvent>, Task<Result<Error>>> func)
    {
        _tasks.Add(func(Events));
    }
}