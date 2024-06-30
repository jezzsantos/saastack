using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Defines a base class for integration events
/// </summary>
public class IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        RootId = null!;
        OccurredUtc = DateTime.UtcNow;
    }

    protected IntegrationEvent(string rootId)
    {
        RootId = rootId;
        OccurredUtc = DateTime.UtcNow;
    }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; }
}