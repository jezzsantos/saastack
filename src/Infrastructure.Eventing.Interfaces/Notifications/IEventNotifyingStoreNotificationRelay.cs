namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines an observer that monitors notification production
/// </summary>
public interface IEventNotifyingStoreNotificationRelay : IDisposable
{
    /// <summary>
    ///     Whether the service is started
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    ///     Starts the service
    /// </summary>
    void Start();
}