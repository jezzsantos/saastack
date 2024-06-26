using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Ancillary.EmailDelivery;
using Created = Domain.Events.Shared.Ancillary.Audits.Created;

namespace AncillaryDomain;

public static class Events
{
    public static class EmailDelivery
    {
        public static Domain.Events.Shared.Ancillary.EmailDelivery.Created Created(Identifier id,
            QueuedMessageId messageId)
        {
            return new Domain.Events.Shared.Ancillary.EmailDelivery.Created(id)
            {
                MessageId = messageId
            };
        }

        public static DeliveryAttempted DeliveryAttempted(Identifier id, DateTime when)
        {
            return new DeliveryAttempted(id)
            {
                When = when
            };
        }

        public static DeliveryFailed DeliveryFailed(Identifier id, DateTime when)
        {
            return new DeliveryFailed(id)
            {
                When = when
            };
        }

        public static DeliverySucceeded DeliverySucceeded(Identifier id, DateTime when)
        {
            return new DeliverySucceeded(id)
            {
                When = when
            };
        }

        public static EmailDetailsChanged EmailDetailsChanged(Identifier id, string subject, string body,
            EmailRecipient to)
        {
            return new EmailDetailsChanged(id)
            {
                Subject = subject,
                Body = body,
                ToEmailAddress = to.EmailAddress,
                ToDisplayName = to.DisplayName
            };
        }
    }

    public static class Audits
    {
        public static Created Created(Identifier id, Identifier againstId, Optional<Identifier> organizationId,
            string auditCode, Optional<string> messageTemplate, TemplateArguments templateArguments)
        {
            return new Created(id)
            {
                OrganizationId = organizationId.HasValue
                    ? organizationId.Value.Text
                    : null,
                AgainstId = againstId,
                AuditCode = auditCode,
                MessageTemplate = messageTemplate.ValueOrDefault ?? string.Empty,
                TemplateArguments = templateArguments.Items
            };
        }
    }
}