using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
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
                        dto.MessageId = e.MessageId;
                        dto.Attempts = DeliveryAttempts.Empty;
                        dto.LastAttempted = Optional<DateTime?>.None;
                        dto.Failed = Optional<DateTime?>.None;
                        dto.Delivered = Optional<DateTime?>.None;
                    },
                    cancellationToken);

            case EmailDetailsChanged e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.Subject = e.Subject;
                    dto.Body = e.Body;
                    dto.ToEmailAddress = e.ToEmailAddress;
                    dto.ToDisplayName = e.ToDisplayName;
                }, cancellationToken);

            case DeliveryAttempted e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto =>
                {
                    var attempts = dto.Attempts.HasValue
                        ? dto.Attempts.Value.Attempt(e.When)
                        : DeliveryAttempts.Create(e.When);
                    if (attempts.IsSuccessful)
                    {
                        dto.Attempts = attempts.Value;
                    }
                    else
                    {
                        dto.Attempts = DeliveryAttempts.Empty;
                    }

                    dto.LastAttempted = e.When;
                }, cancellationToken);

            case DeliveryFailed e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto => { dto.Failed = e.When; },
                    cancellationToken);

            case DeliverySucceeded e:
                return await _deliveries.HandleUpdateAsync(e.RootId, dto => { dto.Delivered = e.When; },
                    cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(EmailDeliveryRoot);
}