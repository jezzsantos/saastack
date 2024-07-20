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

        public static DeliveryConfirmed DeliveryConfirmed(Identifier id, string receiptId, DateTime when)
        {
            return new DeliveryConfirmed(id)
            {
                When = when,
                ReceiptId = receiptId
            };
        }

        public static DeliveryFailureConfirmed DeliveryFailureConfirmed(Identifier id, string receiptId, DateTime when,
            string reason)
        {
            return new DeliveryFailureConfirmed(id)
            {
                When = when,
                ReceiptId = receiptId,
                Reason = reason
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

        public static SendingAttempted SendingAttempted(Identifier id, DateTime when)
        {
            return new SendingAttempted(id)
            {
                When = when
            };
        }

        public static SendingFailed SendingFailed(Identifier id, DateTime when)
        {
            return new SendingFailed(id)
            {
                When = when
            };
        }

        public static SendingSucceeded SendingSucceeded(Identifier id, Optional<string> receiptId, DateTime when)
        {
            return new SendingSucceeded(id)
            {
                When = when,
                ReceiptId = receiptId.HasValue
                    ? receiptId.Value
                    : null
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