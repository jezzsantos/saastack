namespace Infrastructure.Eventing.Interfaces.Projections;

/// <summary>
///     Defines an observer that monitors projection production
/// </summary>
public interface IEventNotifyingStoreProjectionRelay : IDisposable
{
    /// <summary>
    ///     Whether the service is started
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    ///     Starts the observer
    /// </summary>
    void Start();

    /// <summary>
    ///     Stops the observer
    /// </summary>
    void Stop();
}