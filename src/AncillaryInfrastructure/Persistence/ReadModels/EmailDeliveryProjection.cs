using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace AncillaryInfrastructure.Persistence.ReadModels;

public class EmailDeliveryProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<EmailDelivery> _deliveries;

    public EmailDeliveryProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _deliveries = new ReadModelProjectionStore<EmailDelivery>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(EmailDeliveryRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Events.EmailDelivery.Created e:
                return await _deliveries.HandleCreateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.MessageId = e.MessageId;
                        dto.Attempts = DeliveryAttempts.Empty;
                        dto.LastAttempted = Optional<DateTime?>.None;
                        dto.Failed = Optional<DateTime?>.None;
                        dto.Delivered = Optional<DateTime?>.None;
                    },
                    cancellationToken);

            case Events.EmailDelivery.EmailDetailsChanged e:
                return await _deliveries.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.Subject = e.Subject;
                    dto.Body = e.Body;
                    dto.ToEmailAddress = e.ToEmailAddress;
                    dto.ToDisplayName = e.ToDisplayName;
                }, cancellationToken);

            case Events.EmailDelivery.DeliveryAttempted e:
                return await _deliveries.HandleUpdateAsync(e.RootId.ToId(), dto =>
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

            case Events.EmailDelivery.DeliveryFailed e:
                return await _deliveries.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.Failed = e.When; },
                    cancellationToken);

            case Events.EmailDelivery.DeliverySucceeded e:
                return await _deliveries.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.Delivered = e.When; },
                    cancellationToken);

            default:
                return false;
        }
    }
}