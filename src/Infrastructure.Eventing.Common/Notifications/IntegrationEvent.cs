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

    public string RootId { get; set; }

    public DateTime OccurredUtc { get; set; }
}