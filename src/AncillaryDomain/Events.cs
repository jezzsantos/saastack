using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace AncillaryDomain;

public static class Events
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