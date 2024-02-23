using Application.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.Usages)]
public class UsageMessage : QueuedMessage
{
    public Dictionary<string, string>? Additional { get; set; }

    public string? EventName { get; set; }

    public string? ForId { get; set; }
}