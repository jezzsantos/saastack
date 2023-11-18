namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store that can be notified when an event stream changes
/// </summary>
public interface IEventNotifyingStore
{
    /// <summary>
    ///     Fired when one or more events is added to an event stream
    /// </summary>
    event EventStreamChanged OnEventStreamChanged;
}

/// <summary>
///     Defines a delegate for handling changes to an event stream
/// </summary>
public delegate void EventStreamChanged(object sender, EventStreamChangedArgs args);

/// <summary>
///     Defines the arguments for the <see cref="EventStreamChanged" /> delegate
/// </summary>
public sealed class EventStreamChangedArgs
{
    public EventStreamChangedArgs(IReadOnlyList<EventStreamChangeEvent> events)
    {
        Events = events;
    }

    public IReadOnlyList<EventStreamChangeEvent> Events { get; }
}