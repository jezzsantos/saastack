using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Audit : IIdentifiableResource
{
    public string? AgainstId { get; set; }

    public required string AuditCode { get; set; }

    public required string MessageTemplate { get; set; }

    public required string OrganizationId { get; set; }

    public required List<string> TemplateArguments { get; set; }

    public required string Id { get; set; }
}

public enum RecorderTraceLevel
{
    Debug,
    Information,
    Warning,
    Error
}

public class DeliveredEmail : IIdentifiableResource
{
    public List<DateTime> Attempts { get; set; } = new();

    public required string Body { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? FailedDeliveryAt { get; set; }

    public string? FailedDeliveryReason { get; set; }

    public bool IsDelivered { get; set; }

    public bool IsDeliveryFailed { get; set; }

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public required string Subject { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }

    public required string Id { get; set; }
}

public class DomainEvent : IIdentifiableResource
{
    public required string Data { get; set; }

    public required string EventType { get; set; }

    public required string MetadataFullyQualifiedName { get; set; }

    public required string RootAggregateType { get; set; }

    public required string StreamName { get; set; }

    public required int Version { get; set; }

    public required string Id { get; set; }
}