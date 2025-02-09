using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Audit : IIdentifiableResource
{
    public string? AgainstId { get; set; }

    public required string AuditCode { get; set; }

    public required DateTime Created { get; set; }

    public required string MessageTemplate { get; set; }

    public string? OrganizationId { get; set; }

    public required List<string> TemplateArguments { get; set; }

    public required string Id { get; set; }
}

public enum RecorderTraceLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}

public class DeliveredEmail : IIdentifiableResource
{
    public List<DateTime> Attempts { get; set; } = new();

    public required string Body { get; set; }

    public required DateTime Created { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? FailedDeliveryAt { get; set; }

    public string? FailedDeliveryReason { get; set; }

    public bool IsDelivered { get; set; }

    public bool IsDeliveryFailed { get; set; }

    public bool IsSent { get; set; }

    public string? OrganizationId { get; set; }

    public DateTime? SentAt { get; set; }

    public required string Subject { get; set; }

    public required List<string> Tags { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }

    public required string Id { get; set; }
}

public class DeliveredSms : IIdentifiableResource
{
    public List<DateTime> Attempts { get; set; } = new();

    public required string Body { get; set; }

    public required DateTime Created { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? FailedDeliveryAt { get; set; }

    public string? FailedDeliveryReason { get; set; }

    public bool IsDelivered { get; set; }

    public bool IsDeliveryFailed { get; set; }

    public bool IsSent { get; set; }

    public string? OrganizationId { get; set; }

    public DateTime? SentAt { get; set; }

    public required List<string> Tags { get; set; }

    public required string ToPhoneNumber { get; set; }

    public required string Id { get; set; }
}

public class EventNotification : IIdentifiableResource
{
    public required string Data { get; set; }

    public required string EventType { get; set; }

    public required string MetadataFullyQualifiedName { get; set; }

    public required string RootAggregateType { get; set; }

    public required string StreamName { get; set; }

    public required string SubscriberRef { get; set; }

    public required int Version { get; set; }

    public required string Id { get; set; }
}