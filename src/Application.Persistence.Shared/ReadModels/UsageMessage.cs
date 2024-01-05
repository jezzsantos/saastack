using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName("usages")]
public class UsageMessage : QueuedMessage
{
    public Dictionary<string, string>? Additional { get; set; }

    public string? EventName { get; set; }

    public string? ForId { get; set; }
}