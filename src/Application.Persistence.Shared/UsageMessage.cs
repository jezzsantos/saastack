using QueryAny;

namespace Application.Persistence.Shared;

[EntityName("usages")]
public class UsageMessage : QueuedMessage
{
    public Dictionary<string, string>? Context { get; set; }

    public string? EventName { get; set; }

    public string? ForId { get; set; }
}