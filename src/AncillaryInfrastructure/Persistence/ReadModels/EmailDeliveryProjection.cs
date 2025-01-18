using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Events.Shared.Ancillary.EmailDelivery;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace AncillaryInfrastructure.Persistence.ReadModels;

public class EmailDeliveryProjection : IReadModelProjection
{
    private readonly IReadModelStore<EmailDelivery> _deliveries;

    public EmailDeliveryProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _deliveries = new ReadModelStore<EmailDelivery>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _deliveries.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.OrganizationId = e.OrganizationId;
                        dto.MessageId = e.MessageId;
                        dto.Created = e.When;
                        dto.Attempts = SendingAttempts.Empty;
                        dto.LastAttempted = Optional<DateTime?>.None;
                        dto.SendFailed = Optional<DateTime?>.None;
                        dto.Sent = Optional<DateTime?>.None;
                    },
                    cancellationToken);

            case EmailDetailsChanged e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.ContentType = e.ContentType;
                    dto.Subject = e.Subject;
                    dto.Body = e.Body;
                    dto.TemplateId = e.TemplateId;
                    dto.Substitutions = e.Substitutions.ToJson();
                    dto.ToEmailAddress = e.ToEmailAddress;
                    dto.ToDisplayName = e.ToDisplayName;
                    dto.Tags = e.Tags.ToJson();
                }, cancellationToken);

            case SendingAttempted e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                {
                    var attempts = dto.Attempts.HasValue
                        ? dto.Attempts.Value.Attempt(e.When)
                        : SendingAttempts.Create(e.When);
                    if (attempts.IsSuccessful)
                    {
                        dto.Attempts = attempts.Value;
                    }
                    else
                    {
                        dto.Attempts = SendingAttempts.Empty;
                    }

                    dto.LastAttempted = e.When;
                }, cancellationToken);

            case SendingFailed e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto => { dto.SendFailed = e.When; },
                    cancellationToken);

            case SendingSucceeded e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.Sent = e.When;
                        dto.ReceiptId = e.ReceiptId;
                    },
                    cancellationToken);

            case DeliveryConfirmed e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.Delivered = e.When;
                        dto.DeliveryFailed = Optional<DateTime?>.None;
                        dto.DeliveryFailedReason = Optional<string>.None;
                    },
                    cancellationToken);

            case DeliveryFailureConfirmed e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.DeliveryFailed = e.When;
                        dto.Delivered = Optional<DateTime?>.None;
                        dto.DeliveryFailedReason = e.Reason;
                    },
                    cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(EmailDeliveryRoot);
}