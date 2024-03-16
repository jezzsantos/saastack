using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace AncillaryDomain;

public static class Events
{
    public static class EmailDelivery
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, QueuedMessageId messageId)
            {
                return new Created
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    MessageId = messageId
                };
            }

            public required string MessageId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class EmailDetailsChanged : IDomainEvent
        {
            public static EmailDetailsChanged Create(Identifier id, string subject, string body, EmailRecipient to)
            {
                return new EmailDetailsChanged
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    Subject = subject,
                    Body = body,
                    ToEmailAddress = to.EmailAddress,
                    ToDisplayName = to.DisplayName
                };
            }

            public required string Body { get; set; }

            public required string Subject { get; set; }

            public required string ToDisplayName { get; set; }

            public required string ToEmailAddress { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class DeliveryAttempted : IDomainEvent
        {
            public static DeliveryAttempted Create(Identifier id, DateTime when)
            {
                return new DeliveryAttempted
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    When = when
                };
            }

            public required DateTime When { get; set; }

            public required DateTime OccurredUtc { get; set; }

            public required string RootId { get; set; }
        }

        public sealed class DeliveryFailed : IDomainEvent
        {
            public static DeliveryFailed Create(Identifier id, DateTime when)
            {
                return new DeliveryFailed
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    When = when
                };
            }

            public required DateTime When { get; set; }

            public required DateTime OccurredUtc { get; set; }

            public required string RootId { get; set; }
        }

        public sealed class DeliverySucceeded : IDomainEvent
        {
            public static DeliverySucceeded Create(Identifier id, DateTime when)
            {
                return new DeliverySucceeded
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    When = when
                };
            }

            public required DateTime When { get; set; }

            public required DateTime OccurredUtc { get; set; }

            public required string RootId { get; set; }
        }
    }

    public static class Audits
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, Identifier againstId, Optional<Identifier> organizationId,
                string auditCode, Optional<string> messageTemplate, TemplateArguments templateArguments)
            {
                return new Created
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    OrganizationId = organizationId.HasValue
                        ? organizationId.Value.Text
                        : null,
                    AgainstId = againstId,
                    AuditCode = auditCode,
                    MessageTemplate = messageTemplate.ValueOrDefault ?? string.Empty,
                    TemplateArguments = templateArguments.Items
                };
            }

            public required string AgainstId { get; set; }

            public required string AuditCode { get; set; }

            public required string MessageTemplate { get; set; }

            public string? OrganizationId { get; set; }

            public required List<string> TemplateArguments { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }
}