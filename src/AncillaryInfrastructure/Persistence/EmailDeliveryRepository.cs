using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

namespace AncillaryInfrastructure.Persistence;

public class EmailDeliveryRepository : IEmailDeliveryRepository
{
    private readonly IEventSourcingDddCommandStore<EmailDeliveryRoot> _deliveries;
    private readonly ISnapshottingQueryStore<EmailDelivery> _deliveryQueries;

    public EmailDeliveryRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<EmailDeliveryRoot> deliveriesStore, IDataStore store)
    {
        _deliveryQueries = new SnapshottingQueryStore<EmailDelivery>(recorder, domainFactory, store);
        _deliveries = deliveriesStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _deliveryQueries.DestroyAllAsync(cancellationToken),
            _deliveries.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<EmailDeliveryRoot>, Error>> FindDeliveryByMessageIdAsync(
        QueuedMessageId messageId, CancellationToken cancellationToken)
    {
        var query = Query.From<EmailDelivery>()
            .Where<string>(at => at.MessageId, ConditionOperator.EqualTo, messageId);

        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<EmailDeliveryRoot, Error>> SaveAsync(EmailDeliveryRoot delivery, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _deliveries.SaveAsync(delivery, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(delivery.Id, cancellationToken)
            : delivery;
    }

    public async Task<Result<List<EmailDelivery>, Error>> SearchAllDeliveriesAsync(DateTime sinceUtc,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var queried = await _deliveryQueries.QueryAsync(Query.From<EmailDelivery>()
            .Where<DateTime?>(u => u.LastAttempted, ConditionOperator.GreaterThan, sinceUtc)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var deliveries = queried.Value.Results;
        return deliveries;
    }

    private async Task<Result<EmailDeliveryRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var delivery = await _deliveries.LoadAsync(id, cancellationToken);
        if (delivery.IsFailure)
        {
            return delivery.Error;
        }

        return delivery.Value;
    }

    private async Task<Result<Optional<EmailDeliveryRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<EmailDelivery> query,
        CancellationToken cancellationToken)
    {
        var queried = await _deliveryQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<EmailDeliveryRoot>.None;
        }

        var deliveries = await _deliveries.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (deliveries.IsFailure)
        {
            return deliveries.Error;
        }

        return deliveries.Value.ToOptional();
    }
}