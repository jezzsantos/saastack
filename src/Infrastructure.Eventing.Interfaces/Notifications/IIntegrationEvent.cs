namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines an integration event to communicate past events of an aggregate outside the process
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    ///     Returns the time when the event happened
    /// </summary>
    DateTime OccurredUtc { get; set; }

    /// <summary>
    ///     Returns the ID of the root aggregate
    /// </summary>
    string RootId { get; set; }
}