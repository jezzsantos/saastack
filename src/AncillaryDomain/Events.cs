using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace AncillaryDomain;

public static class Events
{
    public static class EmailDelivery
    {
        public class Created : IDomainEvent
        {
            public static Created Create(Identifier id, string messageId)
            {
                return new Created
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    MessageId = messageId,
                };
            }

            public required string MessageId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public class RecipientAdded : IDomainEvent
        {
            public static RecipientAdded Create(Identifier id, EmailAddress to)
            {
                return new RecipientAdded
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    To = to,
                };
            }

            public required string RootId { get; set; }
            public required DateTime OccurredUtc { get; set; }
            public required EmailAddress To;
        }

        public class Attempted : IDomainEvent
        {
            public static Attempted Create(Identifier id, DateTime when)
            {
                return new Attempted()
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    When = when,
                };
            }
            
            public required string RootId { get; set; }
            public required DateTime When { get; set; }
            public required DateTime OccurredUtc { get; set; }
        }

        public class CompletedDelivery : IDomainEvent
        {
            public static CompletedDelivery Create(Identifier id, string transactionId)
            {
                return new CompletedDelivery()
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    TransactionId = transactionId,
                };
            }
            
            public required string RootId { get; set; }
            public required string TransactionId { get; set; }
            public required DateTime OccurredUtc { get; set; }
        }
    }
    
    
    public static class Audits
    {
        public class Created : IDomainEvent
        {
            public static Created Create(Identifier id, Identifier againstId, Identifier organizationId,
                string auditCode, Optional<string> messageTemplate, TemplateArguments templateArguments)
            {
                return new Created
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow,
                    OrganizationId = organizationId,
                    AgainstId = againstId,
                    AuditCode = auditCode,
                    MessageTemplate = messageTemplate.ValueOrDefault ?? string.Empty,
                    TemplateArguments = templateArguments.Items
                };
            }

            public required string AgainstId { get; set; }

            public required string AuditCode { get; set; }

            public required string MessageTemplate { get; set; }

            public required string OrganizationId { get; set; }

            public required List<string> TemplateArguments { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }

}