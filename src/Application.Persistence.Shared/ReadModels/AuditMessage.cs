using Application.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.Audits)]
public class AuditMessage : QueuedMessage
{
    public string? AgainstId { get; set; }

    public List<string>? Arguments { get; set; }

    public string? AuditCode { get; set; }

    public string? MessageTemplate { get; set; }
}