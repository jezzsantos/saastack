using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;

namespace SubscriptionsApplication;

/// <summary>
///     We want to avoid raising errors for failed attempts here, so that Chargebee does not attempt to retry again.
///     Wed ont need to handle a transfer of ownership of the customer, since we dont yet store information about the
///     buyer, beyond just the Id of the Chargebee customer.
/// </summary>
public class ChargebeeApplication : IChargebeeApplication
{
    private readonly IRecorder _recorder;
    private readonly ISubscriptionsApplication _subscriptionsApplication;
    private readonly IWebhookNotificationAuditService _webHookNotificationAuditService;

    public ChargebeeApplication(IRecorder recorder, ISubscriptionsApplication subscriptionsApplication,
        IWebhookNotificationAuditService webHookNotificationAuditService)
    {
        _recorder = recorder;
        _subscriptionsApplication = subscriptionsApplication;
        _webHookNotificationAuditService = webHookNotificationAuditService;
    }

    public async Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, string eventId,
        string eventType, ChargebeeEventContent content, CancellationToken cancellationToken)
    {
        var @event = eventType.ToEnumOrDefault(ChargebeeEventType.Unknown);

        _recorder.TraceInformation(caller.ToCall(), "Chargebee webhook event received: {Event}", eventType);

        var created = await _webHookNotificationAuditService.CreateAuditAsync(caller,
            ChargebeeConstants.AuditSourceName,
            eventId, eventType, content.ToJson(false), cancellationToken);
        if (created.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to audit Chargebee webhook event {Event} with {Code}: {Message}", eventType, created.Error.Code,
                created.Error.Message);
            return created.Error;
        }

        var audit = created.Value;
        switch (@event)
        {
            case ChargebeeEventType.CustomerDeleted:
            {
                var customerId = content.Customer!.Id!;
                var newState = content.ToSubscriptionMetadata();
                return await NotifyBuyerDeletedAsync(caller, audit, customerId, newState,
                    cancellationToken);
            }

            case ChargebeeEventType.CustomerChanged:
            case ChargebeeEventType.PaymentSourceAdded:
            case ChargebeeEventType.PaymentSourceDeleted:
            case ChargebeeEventType.PaymentSourceUpdated:
            case ChargebeeEventType.PaymentSourceExpired:
            {
                var customerId = content.Customer!.Id!;
                var newState = content.ToSubscriptionMetadata();
                return await NotifyBuyerPaymentMethodChangedAsync(caller, audit, customerId, newState,
                    cancellationToken);
            }

            case ChargebeeEventType.SubscriptionChanged:
            case ChargebeeEventType.SubscriptionChangesScheduled:
            case ChargebeeEventType.SubscriptionActivated:
            case ChargebeeEventType.SubscriptionReactivated:
            case ChargebeeEventType.SubscriptionTrialExtended:
            case ChargebeeEventType.SubscriptionScheduledCancellationRemoved:
            case ChargebeeEventType.SubscriptionScheduledChangesRemoved:
            {
                var subscriptionId = content.Subscription!.Id!;
                var newState = content.ToSubscriptionMetadata();
                return await NotifySubscriptionPlanChangedAsync(caller, audit, subscriptionId, newState,
                    cancellationToken);
            }

            case ChargebeeEventType.SubscriptionCancelled:
            case ChargebeeEventType.SubscriptionCancellationScheduled:
            {
                var subscriptionId = content.Subscription!.Id!;
                var newState = content.ToSubscriptionMetadata();
                return await NotifySubscriptionCancelledAsync(caller, audit, subscriptionId, newState,
                    cancellationToken);
            }

            case ChargebeeEventType.SubscriptionDeleted:
            {
                var subscriptionId = content.Subscription!.Id!;
                var newState = content.ToSubscriptionMetadata();
                return await NotifySubscriptionDeletedAsync(caller, audit, subscriptionId, newState,
                    cancellationToken);
            }

            default:
                _recorder.TraceInformation(caller.ToCall(), "Chargebee webhook event ignored: {Event}",
                    eventType);
                return Result.Ok;
        }
    }

    private async Task<Result<Error>> NotifyBuyerDeletedAsync(ICallerContext caller, WebhookNotificationAudit audit,
        string customerId, SubscriptionMetadata newState, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForBuyerAsync(caller,
            customerId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for Chargebee buyer reference {Buyer}, with {Code}: {Message}",
                customerId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var notified = await _subscriptionsApplication.NotifyBuyerDeletedAsync(caller,
            ChargebeeConstants.ProviderName, newState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify buyer deleted for Chargebee buyer reference {Buyer}, with {Code}: {Message}",
                customerId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> NotifyBuyerPaymentMethodChangedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string customerId,
        SubscriptionMetadata newState, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForBuyerAsync(caller,
            customerId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for Chargebee buyer reference {Buyer}, with {Code}: {Message}",
                customerId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var (isUnchanged, changedState) = MergeAndCompare(retrievedState.Value, newState);
        if (isUnchanged)
        {
            return Result.Ok;
        }

        var notified = await _subscriptionsApplication.NotifyBuyerPaymentMethodChangedAsync(caller,
            ChargebeeConstants.ProviderName, changedState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify buyer payment method change for Chargebee buyer reference {Buyer}, with {Code}: {Message}",
                customerId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> NotifySubscriptionCancelledAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string subscriptionId,
        SubscriptionMetadata newState, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForSubscriptionAsync(caller,
            subscriptionId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var (isUnchanged, changedState) = MergeAndCompare(retrievedState.Value, newState);
        if (isUnchanged)
        {
            return Result.Ok;
        }

        var notified = await _subscriptionsApplication.NotifySubscriptionCancelledAsync(caller,
            ChargebeeConstants.ProviderName, changedState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify subscription cancelled for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> NotifySubscriptionDeletedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string subscriptionId,
        SubscriptionMetadata newState, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForSubscriptionAsync(caller,
            subscriptionId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var (isUnchanged, changedState) = MergeAndCompare(retrievedState.Value, newState);
        if (isUnchanged)
        {
            return Result.Ok;
        }

        var notified = await _subscriptionsApplication.NotifySubscriptionDeletedAsync(caller,
            ChargebeeConstants.ProviderName, changedState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify subscription deleted for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> NotifySubscriptionPlanChangedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string subscriptionId,
        SubscriptionMetadata newState, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForSubscriptionAsync(caller,
            subscriptionId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var (isUnchanged, changedState) = MergeAndCompare(retrievedState.Value, newState);
        if (isUnchanged)
        {
            return Result.Ok;
        }

        var notified = await _subscriptionsApplication.NotifySubscriptionPlanChangedAsync(caller,
            ChargebeeConstants.ProviderName, changedState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify subscription plan changed for Chargebee subscription reference {Subscription}, with {Code}: {Message}",
                subscriptionId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private static (bool IsSame, SubscriptionMetadata Merged) MergeAndCompare(SubscriptionMetadata oldState,
        SubscriptionMetadata newState)
    {
        var beforeMergeCopy = new SubscriptionMetadata(oldState);
        oldState.Merge(newState);
        if (oldState.Equals(beforeMergeCopy))
        {
            return (true, oldState);
        }

        return (false, oldState);
    }
}

internal static class ChargebeeApplicationConversionExtensions
{
    public static SubscriptionMetadata ToSubscriptionMetadata(this ChargebeeEventContent content)
    {
        // EXTEND: Add other properties from Chargebee if needed
        var metadata = new SubscriptionMetadata();

        if (content.Customer.Exists())
        {
            // Customer
            metadata[ChargebeeConstants.MetadataProperties.CustomerId] = content.Customer.Id!;
            // PaymentMethod
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.PaymentMethodType,
                content.Customer.PaymentMethod, method => method.Exists() && method.Type.HasValue(),
                method => method!.Type);
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.PaymentMethodStatus,
                content.Customer.PaymentMethod, method => method.Exists() && method.Status.HasValue(),
                method => method!.Status);
        }

        if (content.Subscription.Exists())
        {
            if (content.Subscription.Deleted.HasValue
                && content.Subscription.Deleted.Value)
            {
                return metadata;
            }

            // Subscription
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.SubscriptionId,
                content.Subscription.Id, id => id.HasValue(), id => id);
            metadata[ChargebeeConstants.MetadataProperties.CustomerId] =
                content.Subscription.CustomerId!;
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                content.Subscription.Status, status => status.HasValue(), status => status);
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.SubscriptionDeleted,
                content.Subscription.Deleted, flag => flag.HasValue,
                flag => flag.GetValueOrDefault(false).ToString());
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.CanceledAt,
                content.Subscription.CancelledAt, at => at.HasValue,
                at => at.GetValueOrDefault(0).FromUnixTimestamp().ToIso8601());
            // Invoice
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.BillingAmount,
                content.Subscription.SubscriptionItems,
                items => items.HasAny(), items => items.First().Amount.GetValueOrDefault(0).ToString("G"));
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.CurrencyCode,
                content.Subscription.CurrencyCode, code => code.HasValue(), code => code);
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.NextBillingAt,
                content.Subscription.NextBillingAt, at => at.HasValue,
                at => at.GetValueOrDefault(0).FromUnixTimestamp().ToIso8601());
            // PlanPeriod
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.BillingPeriodValue,
                content.Subscription.BillingPeriod, period => period.HasValue,
                period => period.GetValueOrDefault(0).ToString());
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                content.Subscription.BillingPeriodUnit, unit => unit.HasValue(), unit => unit);
            // Plan
            metadata!.TryAddIfTrue(ChargebeeConstants.MetadataProperties.PlanId,
                content.Subscription.SubscriptionItems, items => items.HasAny(),
                items => items.First().ItemPriceId);
            metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.TrialEnd,
                content.Subscription.TrialEnd, at => at.HasValue,
                at => at.GetValueOrDefault(0).FromUnixTimestamp().ToIso8601());
        }

        return metadata;
    }
}