using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName("audits")]
public class AuditMessage : QueuedMessage
{
    public string? AgainstId { get; set; }

    public List<string>? Arguments { get; set; }

    public string? AuditCode { get; set; }

    public string? MessageTemplate { get; set; }
}