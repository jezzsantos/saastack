using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Resources.Shared;
using Application.Services.Shared;
using ChargeBee.Models;
using ChargeBee.Models.Enums;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using Invoice = Application.Resources.Shared.Invoice;
using Subscription = ChargeBee.Models.Subscription;
using Constants = Infrastructure.External.ApplicationServices.ChargebeeStateInterpreter.Constants;
using Feature = ChargeBee.Models.Feature;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides a service client to the Chargebee API
///     <see href="https://apidocs.chargebee.com/docs/api/" />
/// </summary>
public sealed partial class ChargebeeHttpServiceClient : IBillingGatewayService
{
    internal const string BuyerMetadataId = "BuyerId";
    private static readonly TimeSpan CachedPlansTimeToLive = TimeSpan.FromHours(1);
    private readonly string _initialPlanId;
    private readonly IPricingPlansCache _pricingPlansCache;
    private readonly string _productFamilyId;
    private readonly IRecorder _recorder;
    private readonly IChargebeeClient _serviceClient;

    public ChargebeeHttpServiceClient(IRecorder recorder, IConfigurationSettings settings) : this(recorder,
        new ChargebeeClient(recorder, settings), new InMemPricingPlansCache(CachedPlansTimeToLive),
        settings.Platform.GetString(Constants.StartingPlanIdSettingName),
        settings.Platform.GetString(Constants.ProductFamilyIdSettingName))
    {
    }

    internal ChargebeeHttpServiceClient(IRecorder recorder, IChargebeeClient serviceClient,
        IPricingPlansCache pricingPlansCache, string initialPlanId, string productFamilyId)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _initialPlanId = initialPlanId;
        _productFamilyId = productFamilyId;
        _pricingPlansCache = pricingPlansCache;
    }

    /// <summary>
    ///     Cancels the subscription.
    ///     Note1: We first fetch the latest subscription from Chargebee,
    ///     just in case it has already changed from the state we have now.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        if (options.IsInvalidParameter(IsScheduledOrImmediate, nameof(options),
                Resources.ChargebeeHttpServiceClient_Cancel_ScheduleInvalid, out var error))
        {
            return error;
        }

        var startingState = provider.State;
        var subscriptionId = GetSubscriptionId(startingState);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var retrievedSubscription = await GetSubscriptionInternalAsync(caller, startingState, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        var endOfTerm = false;
        Optional<long> cancelAt = default;
        switch (options.CancelWhen)
        {
            case CancelSubscriptionSchedule.Immediately:
                break;

            case CancelSubscriptionSchedule.EndOfTerm:
                endOfTerm = true;
                break;

            case CancelSubscriptionSchedule.Scheduled:
                cancelAt = options.FutureTime!.Value.ToUnixSeconds();
                break;
        }

        var canceledSubscription =
            await _serviceClient.CancelSubscriptionAsync(caller, subscriptionId.Value, endOfTerm, cancelAt,
                cancellationToken);
        if (canceledSubscription.IsFailure)
        {
            return canceledSubscription.Error;
        }

        var subscription = canceledSubscription.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Canceled Chargebee subscription {Subscription}", subscriptionId);

        return subscription.ToSubscriptionState();

        bool IsScheduledOrImmediate(CancelSubscriptionOptions opts)
        {
            return opts.CancelWhen switch
            {
                CancelSubscriptionSchedule.Immediately => opts.FutureTime.NotExists(),
                CancelSubscriptionSchedule.EndOfTerm => opts.FutureTime.NotExists(),
                CancelSubscriptionSchedule.Scheduled => opts.FutureTime.Exists()
                                                        && opts.FutureTime.Value.IsAfter(DateTime.UtcNow),
                _ => false
            };
        }
    }

    /// <summary>
    ///     Changes the plan for the subscription.
    ///     Note1: We first fetch the latest subscription from Chargebee,
    ///     just in case it has already changed from the state we have now.
    ///     Then we do the next best thing to restore or recreate the subscription if it has been canceled
    ///     recently, is now canceled or is unsubscribed.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var startingState = provider.State;
        var customerId = GetCustomerId(startingState);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        var subscriptionId = GetSubscriptionId(startingState);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var startingStatus = startingState.ToStatus();
        if (startingStatus.IsFailure)
        {
            return startingStatus.Error;
        }

        var status = startingStatus.Value.Status;
        var updatedState = startingState;
        if (status != BillingSubscriptionStatus.Unsubscribed)
        {
            var retrievedSubscription = await GetSubscriptionInternalAsync(caller, startingState, cancellationToken);
            if (retrievedSubscription.IsFailure)
            {
                return retrievedSubscription.Error;
            }

            updatedState = retrievedSubscription.Value;
            var retrievedStatus = updatedState.ToStatus();
            if (retrievedStatus.IsFailure)
            {
                return retrievedStatus.Error;
            }

            status = retrievedStatus.Value.Status;
        }

        Result<SubscriptionMetadata, Error> modifiedSubscription = updatedState;
        switch (status)
        {
            case BillingSubscriptionStatus.Activated:
                break;

            case BillingSubscriptionStatus.Canceling:
            {
                modifiedSubscription =
                    await RemoveScheduledCancellationInternalAsync(caller, updatedState, cancellationToken);
                break;
            }

            case BillingSubscriptionStatus.Canceled:
            {
                modifiedSubscription =
                    await ReactivateSubscriptionInternalAsync(caller, updatedState, cancellationToken);
                break;
            }

            case BillingSubscriptionStatus.Unsubscribed:
            {
                var subscriber = options.Subscriber;
                modifiedSubscription = await CreateSubscriptionForCustomerInternalAsync(caller, updatedState,
                    subscriber, _initialPlanId, SubscribeOptions.Immediately, DateTime.UnixEpoch,
                    cancellationToken);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (modifiedSubscription.IsFailure)
        {
            return modifiedSubscription.Error;
        }

        updatedState = modifiedSubscription.Value;
        var changedSubscription = await ChangePlanInternalAsync(caller, options, updatedState, cancellationToken);
        if (changedSubscription.IsFailure)
        {
            return changedSubscription.Error;
        }

        updatedState = changedSubscription.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Changed Chargebee subscription {Subscription} to plan {Plan}", subscriptionId, options.PlanId);

        return updatedState;
    }

    /// <summary>
    ///     Builds up all the pricing plans for the product family, and caches them for future use.
    ///     Assumes that some plans will have zero or more setup costs, and zero or more features.
    ///     Note: Building these plans is very expensive (in terms of the number of API calls necessary),
    ///     so we will cache them for some time.
    /// </summary>
    public async Task<Result<PricingPlans, Error>> ListAllPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var cachedPlans = await _pricingPlansCache.GetAsync(cancellationToken);
        if (cachedPlans.HasValue)
        {
            return cachedPlans.Value;
        }

        var retrievedItemPrices =
            await _serviceClient.ListActiveItemPricesAsync(caller, _productFamilyId, cancellationToken);
        if (retrievedItemPrices.IsFailure)
        {
            return retrievedItemPrices.Error;
        }

        var itemPrices = retrievedItemPrices.Value;
        _recorder.TraceInformation(caller.ToCall(), "Listed Chargebee for {Count} plans for family {ProductFamily}",
            itemPrices.Count, _productFamilyId);

        var retrievedFeatures = await _serviceClient.ListSwitchFeaturesAsync(caller, cancellationToken);
        if (retrievedFeatures.IsFailure)
        {
            return retrievedFeatures.Error;
        }

        var allFeatures = retrievedFeatures.Value;
        _recorder.TraceInformation(caller.ToCall(), "Listed Chargebee for {Count} features", itemPrices.Count);

        var allPlans = new List<PricingPlan>();
        foreach (var planItemPrice in itemPrices.Where(ip => ip.ItemType == ItemTypeEnum.Plan))
        {
            var retrievedSetupCost = await GetPlanSetupCost(planItemPrice);
            if (retrievedSetupCost.IsFailure)
            {
                return retrievedSetupCost.Error;
            }

            var planSetupCost = retrievedSetupCost.Value;
            var retrievedPlanFeatures = await GetPlanFeatures(planItemPrice);
            if (retrievedPlanFeatures.IsFailure)
            {
                return retrievedPlanFeatures.Error;
            }

            var planFeatures = retrievedPlanFeatures.Value;
            var planCost = CurrencyCodes.FromMinorUnit(planItemPrice.CurrencyCode,
                (int)planItemPrice.Price.GetValueOrDefault(0));
            var plan = planItemPrice.ToPricingPlan(planFeatures, planCost, planSetupCost);
            allPlans.Add(plan);
        }

        var plans = new PricingPlans
        {
            Daily = allPlans.Where(plan => plan.Period.Unit == PeriodFrequencyUnit.Day)
                .OrderBy(plan => plan.Cost)
                .ToList(),
            Weekly = allPlans.Where(plan => plan.Period.Unit == PeriodFrequencyUnit.Week)
                .OrderBy(plan => plan.Cost)
                .ToList(),
            Monthly = allPlans.Where(plan => plan.Period.Unit == PeriodFrequencyUnit.Month)
                .OrderBy(plan => plan.Cost)
                .ToList(),
            Annually = allPlans.Where(plan => plan.Period.Unit == PeriodFrequencyUnit.Year)
                .OrderBy(plan => plan.Cost)
                .ToList(),
            Eternally = allPlans.Where(plan => plan.Period.Unit == PeriodFrequencyUnit.Eternity)
                .OrderBy(plan => plan.Cost)
                .ToList()
        };

        await _pricingPlansCache.SetAsync(plans, cancellationToken);
        return plans;

        async Task<Result<decimal, Error>> GetPlanSetupCost(ItemPrice planItemPrice)
        {
            var retrievedCharges =
                await _serviceClient.ListPlanChargesAsync(caller, planItemPrice.ItemId, cancellationToken);
            if (retrievedCharges.IsFailure)
            {
                return retrievedCharges.Error;
            }

            var charges = retrievedCharges.Value;
            if (charges.HasAny())
            {
                var setupCharges = charges
                    .Where(attachment => attachment is
                    {
                        Status: AttachedItem.StatusEnum.Active,
                        ChargeOnEvent: ChargeOnEventEnum.SubscriptionCreation
                    })
                    .ToList();

                var currency = planItemPrice.CurrencyCode;
                var prices = LookupChargePriceItemInSameCurrency();

                return prices.Sum(price =>
                    CurrencyCodes.FromMinorUnit(currency, (int)price.Price.GetValueOrDefault(0)));

                List<ItemPrice> LookupChargePriceItemInSameCurrency()
                {
                    return setupCharges
                        .Select(charge =>
                            itemPrices.FirstOrDefault(ip => ip.ItemType == ItemTypeEnum.Charge
                                                            && ip.ItemId == charge.ItemId
                                                            && ip.CurrencyCode == currency))
                        .Where(price => price.Exists())
                        .ToList()!;
                }
            }

            return 0M;
        }

        async Task<Result<IReadOnlyList<Feature>, Error>> GetPlanFeatures(ItemPrice planItemPrice)
        {
            var retrievedEntitlements =
                await _serviceClient.ListPlanEntitlementsAsync(caller, planItemPrice.ItemId, cancellationToken);
            if (retrievedEntitlements.IsFailure)
            {
                return retrievedEntitlements.Error;
            }

            var entitlements = retrievedEntitlements.Value;
            var features = new List<Feature>();
            if (entitlements.HasAny())
            {
                foreach (var entitlement in entitlements)
                {
                    var itemFeature = allFeatures.FirstOrDefault(feature => feature.Id == entitlement.FeatureId);
                    if (itemFeature.Exists())
                    {
                        features.Add(itemFeature);
                    }
                }
            }

            return features;
        }
    }

    /// <summary>
    ///     Creates a new customer to restore an existing customer that is now presumed deleted in Chargebee.
    ///     We make sure that this customer does not already exist.
    ///     See <see cref="SubscribeAsync" /> for more details about how new customers are created.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> RestoreBuyerAsync(ICallerContext caller,
        SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        var updatedCustomer = await UpsertCustomerFromBuyerInternalAsync(caller, buyer, cancellationToken);
        if (updatedCustomer.IsFailure)
        {
            return updatedCustomer.Error;
        }

        var updatedState = updatedCustomer.Value;
        var customerId = GetCustomerId(updatedState);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Restored Chargebee customer {Customer}", customerId);

        return updatedState;
    }

    /// <summary>
    ///     Searches for all invoices for the customer, given the specified date range, and options
    /// </summary>
    public async Task<Result<SearchResults<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller,
        BillingProvider provider, DateTime fromUtc, DateTime toUtc, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId(provider.State);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        var retrievedInvoices = await _serviceClient.SearchAllCustomerInvoicesAsync(caller, customerId.Value, fromUtc,
            toUtc, searchOptions, cancellationToken);
        if (retrievedInvoices.IsFailure)
        {
            return retrievedInvoices.Error;
        }

        var invoices = retrievedInvoices.Value.ToList();
        _recorder.TraceInformation(caller.ToCall(), "Searched Chargebee for {Count} invoices for {Customer}",
            invoices.Count, customerId);

        return invoices
            .ConvertAll(invoice => invoice.ToInvoice())
            .ToSearchResults(searchOptions);
    }

    /// <summary>
    ///     Subscribes the buyer with a new subscription, and a new customer (if needed).
    ///     In Chargebee, that is a new customer for the buyer, and a new subscription for the subscription, for that customer.
    ///     Note: When creating a new customer in CB, we can define metadata for that customer that can link it back to the
    ///     buyer. Chargebee also allows us to provide our own identifier for the customer, so we will use the OwningEntityId
    ///     as a handy reference to use in the Chargebee portal for administrators.
    ///     Note: There should only ever be one CB customer per Organization in this product. If a customer in CB is ever
    ///     deleted (by accident) then this unsubscribes the Subscription in the product, forcing it to subscribe again, and
    ///     create a new CB Customer record. Hence, we always create a new CB Customer record for every Subscribe.
    ///     Note: When creating a new subscription in CB, we can define metadata for that subscription that can link it back to
    ///     subscription. There can be many CB subscriptions over time, for the same Subscription in the product, since
    ///     subscriptions in CB can be deleted.
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller,
        SubscriptionBuyer buyer, SubscribeOptions options, CancellationToken cancellationToken)
    {
        var updatedCustomer = await UpsertCustomerFromBuyerInternalAsync(caller, buyer, cancellationToken);
        if (updatedCustomer.IsFailure)
        {
            return updatedCustomer.Error;
        }

        var planId =
#if TESTINGONLY
            options.PlanId.HasValue()
                ? options.PlanId
                : _initialPlanId;
#else
            _initialPlanId;
#endif
        var updatedState = updatedCustomer.Value;
        var createdSubscription = await CreateSubscriptionForCustomerInternalAsync(caller, updatedState,
            buyer.Subscriber, planId, options, Optional<DateTime>.None, cancellationToken);
        if (createdSubscription.IsFailure)
        {
            return createdSubscription.Error;
        }

        updatedState = createdSubscription.Value;
        var customerId = GetCustomerId(updatedState);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        var subscriptionId = GetSubscriptionId(updatedState);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Subscribed Chargebee customer {Customer} to subscription {Subscription} on plan {Plan}",
            customerId, subscriptionId, planId);

        return updatedState;
    }

    /// <summary>
    ///     Transfers the subscription to another buyer (and possibly changes the plan)
    /// </summary>
    public async Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        if (options.IsInvalidParameter(HasBuyerReference, nameof(options),
                Resources.ChargebeeHttpServiceClient_Transfer_BuyerInvalid, out var error))
        {
            return error;
        }

        var startingState = provider.State;
        var customerId = GetCustomerId(startingState);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        var subscriptionId = GetSubscriptionId(startingState);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var startingStatus = startingState.ToStatus();
        if (startingStatus.IsFailure)
        {
            return startingStatus.Error;
        }

        var status = startingStatus.Value.Status;
        var updatedState = startingState;
        if (status != BillingSubscriptionStatus.Unsubscribed)
        {
            var retrievedSubscription = await GetSubscriptionInternalAsync(caller, startingState, cancellationToken);
            if (retrievedSubscription.IsFailure)
            {
                return retrievedSubscription.Error;
            }

            updatedState = retrievedSubscription.Value;
            var retrievedStatus = updatedState.ToStatus();
            if (retrievedStatus.IsFailure)
            {
                return retrievedStatus.Error;
            }

            status = retrievedStatus.Value.Status;
        }

        var toBuyerId = options.TransfereeBuyer.Id;
        Result<SubscriptionMetadata, Error> modifiedSubscription = updatedState;
        switch (status)
        {
            case BillingSubscriptionStatus.Activated:
            case BillingSubscriptionStatus.Canceled:
            case BillingSubscriptionStatus.Canceling:
                break;

            case BillingSubscriptionStatus.Unsubscribed:
            {
                var planId = options.PlanId.HasValue()
                    ? options.PlanId
                    : _initialPlanId;
                modifiedSubscription = await CreateSubscriptionForCustomerInternalAsync(caller, updatedState,
                    options.TransfereeBuyer.Subscriber, planId, SubscribeOptions.Immediately, DateTime.UnixEpoch,
                    cancellationToken);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (modifiedSubscription.IsFailure)
        {
            return modifiedSubscription.Error;
        }

        updatedState = modifiedSubscription.Value;
        var updatedCustomer = await UpdateCustomerInternalAsync(caller, options.TransfereeBuyer, cancellationToken);
        if (updatedCustomer.IsFailure)
        {
            return updatedCustomer.Error;
        }

        updatedState.Merge(updatedCustomer.Value);
        _recorder.TraceInformation(caller.ToCall(),
            "Transferred Chargebee subscription {Subscription} to {To}", subscriptionId, toBuyerId);

        return updatedState;

        bool HasBuyerReference(TransferSubscriptionOptions opts)
        {
            return opts.TransfereeBuyer.Subscriber.Exists()
                   && opts.TransfereeBuyer.Subscriber.EntityId.HasValue();
        }
    }

    private async Task<Result<SubscriptionMetadata, Error>> ChangePlanInternalAsync(ICallerContext caller,
        ChangePlanOptions options, SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var subscriptionId = GetSubscriptionId(state);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var trialEndsIn = GetFutureTrialEndIfInTrial(state);
        var planId = options.PlanId;

        var changed = await _serviceClient.ChangeSubscriptionPlanAsync(caller, subscriptionId.Value, planId,
            trialEndsIn, cancellationToken);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var subscription = changed.Value;
        _recorder.TraceInformation(caller.ToCall(), "Chargebee changed subscription {Subscription} to plan {Plan}",
            subscription.Id, planId);
        return subscription.ToSubscriptionState();
    }

    /// <summary>
    ///     Returns the end of the trial period if the subscription is still in trial.
    ///     Note: the trial is stored as a Unix timestamp.
    /// </summary>
    private static Optional<long> GetFutureTrialEndIfInTrial(SubscriptionMetadata state)
    {
        if (!state.TryGetValue(ChargebeeConstants.MetadataProperties.TrialEnd, out var trialEnd))
        {
            return Optional<long>.None;
        }

        var unixTimeStamp = trialEnd.ToLongOrDefault(-1);
        if (unixTimeStamp == -1)
        {
            return Optional<long>.None;
        }

        if (unixTimeStamp.FromUnixTimestamp().IsAfter(DateTime.UtcNow))
        {
            return unixTimeStamp;
        }

        return Optional<long>.None;
    }

    private async Task<Result<SubscriptionMetadata, Error>> RemoveScheduledCancellationInternalAsync(
        ICallerContext caller, SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var subscriptionId = GetSubscriptionId(state);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var retrievedSubscription =
            await _serviceClient.RemoveScheduledSubscriptionCancellationAsync(caller, subscriptionId.Value,
                cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        var subscription = retrievedSubscription.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Removed scheduled cancellation of Chargebee subscription {Subscription}", subscription.Id);

        return subscription.ToSubscriptionState();
    }

    private async Task<Result<SubscriptionMetadata, Error>> ReactivateSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var subscriptionId = GetSubscriptionId(state);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var trialEndsIn = GetFutureTrialEndIfInTrial(state);
        var retrievedSubscription =
            await _serviceClient.ReactivateSubscriptionAsync(caller, subscriptionId.Value, trialEndsIn,
                cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        var subscription = retrievedSubscription.Value;
        _recorder.TraceInformation(caller.ToCall(), "Re-activated canceled Chargebee subscription {Subscription}",
            subscription.Id);

        return subscription.ToSubscriptionState();
    }

    private async Task<Result<SubscriptionMetadata, Error>> GetSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionMetadata state, CancellationToken cancellationToken)
    {
        var subscriptionId = GetSubscriptionId(state);
        if (subscriptionId.IsFailure)
        {
            return subscriptionId.Error;
        }

        var retrievedSubscription = await _serviceClient.FindSubscriptionByIdAsync(caller, subscriptionId.Value,
            cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound(
                Resources.ChargebeeHttpServiceClient_SubscriptionNotFound.Format(subscriptionId));
        }

        var subscription = retrievedSubscription.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Fetched Chargebee subscription {Subscription}", subscription.Id);

        return subscription.ToSubscriptionState();
    }

    private static Result<string, Error> GetCustomerId(SubscriptionMetadata state)
    {
        if (state.TryGetValue(ChargebeeConstants.MetadataProperties.CustomerId, out var customerId))
        {
            return customerId;
        }

        return Error.Validation(Resources.ChargebeeHttpServiceClient_InvalidCustomerId);
    }

    private static Result<string, Error> GetSubscriptionId(SubscriptionMetadata state)
    {
        if (state.TryGetValue(ChargebeeConstants.MetadataProperties.SubscriptionId, out var subscriptionId))
        {
            return subscriptionId;
        }

        return Error.Validation(Resources.ChargebeeHttpServiceClient_InvalidSubscriptionId);
    }

    private async Task<Result<SubscriptionMetadata, Error>> CreateSubscriptionForCustomerInternalAsync(
        ICallerContext caller, SubscriptionMetadata state, Subscriber subscriber, string planId,
        SubscribeOptions options, Optional<DateTime> forceEndTrial, CancellationToken cancellationToken)
    {
        subscriber.ThrowIfNullParameter(nameof(subscriber), Resources.ChargebeeHttpServiceClient_InvalidSubscriber);
        planId.ThrowIfNotValuedParameter(nameof(planId), Resources.ChargebeeHttpServiceClient_InvalidPlanId);
        if (options.IsInvalidParameter(IsScheduledOrImmediate, nameof(options),
                Resources.ChargebeeHttpServiceClient_Subscribe_ScheduleInvalid, out var error))
        {
            return error;
        }

        var customerId = GetCustomerId(state);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        var start = GetScheduledStartDate();
        var trialEnds = GetTrialEndDate();
        var created =
            await _serviceClient.CreateSubscriptionForCustomerAsync(caller, customerId.Value,
                subscriber, planId, start, trialEnds, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var subscription = created.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Created Chargebee subscription {Subscription} on plan {Plan} for customer {Customer}",
            subscription.Id, planId, subscription.CustomerId);

        return subscription.ToSubscribedCustomerState(state);

        Optional<long> GetTrialEndDate()
        {
            return forceEndTrial.HasValue &&
                   (forceEndTrial.Value.IsAfter(DateTime.UtcNow) || forceEndTrial == DateTime.UnixEpoch)
                ? forceEndTrial.Value.ToUnixSeconds()
                : Optional<long>.None;
        }

        Optional<long> GetScheduledStartDate()
        {
            if (options.StartWhen == StartSubscriptionSchedule.Scheduled)
            {
                return options.FutureTime.HasValue &&
                       options.FutureTime.Value.IsAfter(DateTime.UtcNow)
                    ? options.FutureTime.Value.ToUnixSeconds()
                    : Optional<long>.None;
            }

            return Optional<long>.None;
        }

        bool IsScheduledOrImmediate(SubscribeOptions opts)
        {
            return opts.StartWhen switch
            {
                StartSubscriptionSchedule.Immediately => opts.FutureTime.NotExists(),
                StartSubscriptionSchedule.Scheduled => opts.FutureTime.Exists()
                                                       && opts.FutureTime.Value.IsAfter(DateTime.UtcNow),
                _ => false
            };
        }
    }

    private async Task<Result<SubscriptionMetadata, Error>> UpsertCustomerFromBuyerInternalAsync(ICallerContext caller,
        SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        var customerId = buyer.MakeCustomerId();
        var buyerId = buyer.Id;

        var retrievedCustomer = await _serviceClient.FindCustomerByIdAsync(caller, customerId, cancellationToken);
        if (retrievedCustomer.IsFailure)
        {
            return retrievedCustomer.Error;
        }

        if (retrievedCustomer.Value.HasValue)
        {
            return await UpdateCustomerInternalAsync(caller, buyer, cancellationToken);
        }

        var created = await _serviceClient.CreateCustomerForBuyerAsync(caller, customerId, buyer, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var customer = created.Value;
        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee customer {Customer} for buyer {Buyer}",
            customer.Id, buyerId);

        return customer.ToCustomerState();
    }

    private async Task<Result<SubscriptionMetadata, Error>> UpdateCustomerInternalAsync(ICallerContext caller,
        SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        var customerId = buyer.MakeCustomerId();
        var buyerId = buyer.Id;

        var updated = await _serviceClient.UpdateCustomerForBuyerAsync(caller, customerId, buyer, cancellationToken);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        var customer = updated.Value;
        _recorder.TraceInformation(caller.ToCall(), "Updated Chargebee customer {Customer} for buyer {Buyer}",
            customer.Id, buyerId);

        var addressUpdated =
            await _serviceClient.UpdateCustomerForBuyerBillingAddressAsync(caller, customerId, buyer,
                cancellationToken);
        if (addressUpdated.IsFailure)
        {
            return addressUpdated.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Updated Chargebee customer billing address for customer {Customer} and buyer {Buyer}",
            customer.Id, buyerId);

        return addressUpdated.Value.ToCustomerState();
    }

    /// <summary>
    ///     Defines a cache for remembering pricing plans
    /// </summary>
    public interface IPricingPlansCache
    {
        /// <summary>
        ///     Returns the cached plans
        /// </summary>
        Task<Optional<PricingPlans>> GetAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Sets the cached plans
        /// </summary>
        Task SetAsync(PricingPlans plans, CancellationToken cancellationToken);
    }

    /// <summary>
    ///     Provides an in-memory cache for fetched pricing plans
    /// </summary>
    internal class InMemPricingPlansCache : IPricingPlansCache
    {
        private readonly TimeSpan _timeToLive;
        private DateTime? _lastCached;
        private PricingPlans? _plans;

        public InMemPricingPlansCache(TimeSpan timeToLive)
        {
            _lastCached = null;
            _plans = null;
            _timeToLive = timeToLive;
        }

        public Task<Optional<PricingPlans>> GetAsync(CancellationToken cancellationToken)
        {
            if (IsExpired())
            {
                _plans = null;
            }

            var plans = _plans.Exists()
                ? _plans.ToOptional()
                : Optional<PricingPlans>.None;
            return Task.FromResult(plans);
        }

        public Task SetAsync(PricingPlans plans, CancellationToken cancellationToken)
        {
            _plans = plans;
            _lastCached = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        private bool IsExpired()
        {
            if (!_lastCached.HasValue)
            {
                return true;
            }

            var now = DateTime.UtcNow;
            return now.IsAfter(_lastCached.Value.Add(_timeToLive));
        }
    }
}

internal static class ChargebeeServiceClientConversionExtensions
{
    public static string GetSubscriberId(this SubscriptionBuyer buyer, string customerId)
    {
        return buyer.Subscriber.EntityId.HasValue()
            ? buyer.Subscriber.EntityId
            : customerId;
    }

    /// <summary>
    ///     Returns a Customer ID that is valid in Chargebee.
    ///     Note: Must be no more than 50 chars long.
    /// </summary>
    public static string MakeCustomerId(this SubscriptionBuyer buyer)
    {
        var entityId = buyer.Subscriber.EntityId;
        return entityId[..Math.Min(entityId.Length, 50)];
    }

    /// <summary>
    ///     Returns a Subscription ID that is valid in Chargebee.
    ///     Note: Must be no more than 50 chars long.
    /// </summary>
    public static string MakeSubscriptionId(this string customerId)
    {
        var random = Guid.NewGuid().ToString("N");
        var id = $"{customerId}.{random}";
        return id[..Math.Min(id.Length, 50)];
    }

    public static SubscriptionMetadata ToCustomerState(this Customer customer)
    {
        var metadata = new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, customer.Id }
        };
        metadata.AppendPaymentMethod(customer);

        return metadata;
    }

    public static Invoice ToInvoice(this ChargeBee.Models.Invoice invoice)
    {
        var status = invoice.Status.ToInvoiceStatus();
        var (periodStart, periodEnd) = GetSpanningPeriod();

        return new Invoice
        {
            Id = invoice.Id,
            Amount = invoice.Total.ToCurrency(invoice.CurrencyCode),
            Currency = invoice.CurrencyCode,
            IncludesTax = invoice.PriceType == PriceTypeEnum.TaxInclusive,
            InvoicedOnUtc = invoice.Date?.ToUniversalTime(),
            LineItems = invoice.LineItems.Select(item => new InvoiceLineItem
            {
                Reference = item.Id,
                Description = item.Description,
                Amount = item.Amount.ToCurrency(invoice.CurrencyCode),
                Currency = invoice.CurrencyCode,
                IsTaxed = item.IsTaxed,
                TaxAmount = item.TaxAmount.ToCurrency(invoice.CurrencyCode)
            }).ToList(),
            Notes = invoice.Notes.HasAny()
                ? invoice.Notes.Select(note => new InvoiceNote
                {
                    Description = note.Note
                }).ToList()
                : [],
            Status = status,
            TaxAmount = ((long?)invoice.Tax).ToCurrency(invoice.CurrencyCode),
            Payment = status == InvoiceStatus.Paid && invoice.LinkedPayments.HasAny()
                ? new InvoiceItemPayment
                {
                    Amount = invoice.AmountPaid.ToCurrency(invoice.CurrencyCode),
                    Currency = invoice.CurrencyCode,
                    PaidOnUtc = invoice.PaidAt?.ToUniversalTime(),
                    Reference = invoice.LinkedPayments.First().TxnId
                }
                : null,
            PeriodEndUtc = periodEnd?.ToUniversalTime(),
            PeriodStartUtc = periodStart?.ToUniversalTime()
        };

        (DateTime? periodStart, DateTime? periodEnd) GetSpanningPeriod()
        {
            if (invoice.LineItems.HasNone())
            {
                return (null, null);
            }

            var validItems = invoice.LineItems
                .Where(item => item.DateFrom.HasValue() || item.DateTo.HasValue());

            var starting = validItems
                .Where(item => item.DateFrom.HasValue())
                .Min(item => item.DateFrom);
            var ending = invoice.LineItems
                .Where(item => item.DateTo.HasValue())
                .Max(item => item.DateTo);

            if (starting.HasValue() && ending.HasValue())
            {
                return (starting, ending);
            }

            return (null, null);
        }
    }

    public static PricingPlan ToPricingPlan(this ItemPrice itemPrice, IReadOnlyList<Feature> features, decimal cost,
        decimal setupCost)
    {
        var trialPeriod = itemPrice.TrialPeriod.GetValueOrDefault(0);

        return new PricingPlan
        {
            Period = new PlanPeriod
            {
                Frequency = itemPrice.Period.GetValueOrDefault(0),
                Unit = itemPrice.PeriodUnit.ToPeriodUnit()
            },
            Cost = cost,
            SetupCost = setupCost,
            Currency = itemPrice.CurrencyCode,
            Description = itemPrice.Description,
            DisplayName = itemPrice.ExternalName,
            FeatureSection = features.ToFeatures(),
            IsRecommended = false,
            Notes = itemPrice.InvoiceNotes,
            Trial = trialPeriod > 0
                ? new SubscriptionTrialPeriod
                {
                    Frequency = trialPeriod,
                    HasTrial = true,
                    Unit = itemPrice.TrialPeriodUnit.ToPeriodUnit()
                }
                : null,
            Id = itemPrice.Id
        };
    }

    public static SubscriptionMetadata ToSubscribedCustomerState(this Subscription subscription,
        SubscriptionMetadata state)
    {
        state.AppendSubscription(subscription);

        return state;
    }

    public static SubscriptionMetadata ToSubscriptionState(this Subscription subscription)
    {
        var metadata = new SubscriptionMetadata();
        metadata.AppendSubscription(subscription);

        return metadata;
    }

    private static decimal? ToCurrency(this long? amountInCents, string currencyCode)
    {
        if (!amountInCents.HasValue)
        {
            return null;
        }

        return CurrencyCodes.FromMinorUnit(currencyCode, (int)amountInCents);
    }

    private static List<PricingFeatureSection> ToFeatures(this IReadOnlyList<Feature> features)
    {
        return features.Select(feature => new PricingFeatureSection
        {
            Features =
            [
                new PricingFeatureItem
                {
                    Description = feature.Description,
                    IsIncluded = true
                }
            ]
        }).ToList();
    }

    private static PeriodFrequencyUnit ToPeriodUnit(this ItemPrice.TrialPeriodUnitEnum? unit)
    {
        return unit switch
        {
            ItemPrice.TrialPeriodUnitEnum.Day => PeriodFrequencyUnit.Day,
            ItemPrice.TrialPeriodUnitEnum.Month => PeriodFrequencyUnit.Month,
            _ => PeriodFrequencyUnit.Eternity
        };
    }

    private static PeriodFrequencyUnit ToPeriodUnit(this ItemPrice.PeriodUnitEnum? unit)
    {
        return unit switch
        {
            ItemPrice.PeriodUnitEnum.Day => PeriodFrequencyUnit.Day,
            ItemPrice.PeriodUnitEnum.Week => PeriodFrequencyUnit.Week,
            ItemPrice.PeriodUnitEnum.Month => PeriodFrequencyUnit.Month,
            ItemPrice.PeriodUnitEnum.Year => PeriodFrequencyUnit.Year,
            _ => PeriodFrequencyUnit.Eternity
        };
    }

    private static InvoiceStatus ToInvoiceStatus(this ChargeBee.Models.Invoice.StatusEnum status)
    {
        return status switch
        {
            ChargeBee.Models.Invoice.StatusEnum.Paid => InvoiceStatus.Paid,
            ChargeBee.Models.Invoice.StatusEnum.Posted => InvoiceStatus.Unpaid,
            ChargeBee.Models.Invoice.StatusEnum.PaymentDue => InvoiceStatus.Unpaid,
            ChargeBee.Models.Invoice.StatusEnum.NotPaid => InvoiceStatus.Unpaid,
            ChargeBee.Models.Invoice.StatusEnum.Voided => InvoiceStatus.Unpaid,
            ChargeBee.Models.Invoice.StatusEnum.Pending => InvoiceStatus.Unpaid,
            _ => InvoiceStatus.Unpaid
        };
    }

    private static void AppendSubscription(this SubscriptionMetadata metadata, Subscription subscription)
    {
        if (subscription.Deleted)
        {
            return;
        }

        metadata[ChargebeeConstants.MetadataProperties.SubscriptionId] = subscription.Id;
        metadata.TryAdd(ChargebeeConstants.MetadataProperties.CustomerId, subscription.CustomerId);
        metadata.AppendPlanPeriod(subscription);
        metadata[ChargebeeConstants.MetadataProperties.SubscriptionStatus] = subscription.Status.ToString();
        metadata[ChargebeeConstants.MetadataProperties.SubscriptionDeleted] = subscription.Deleted.ToString();
        metadata.AppendPlan(subscription);
        metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.CanceledAt, subscription.CancelledAt,
            time => time.HasValue,
            time => time!.Value.ToIso8601());
        metadata.AppendInvoice(subscription);
    }

    private static void AppendPlanPeriod(this SubscriptionMetadata metadata, Subscription subscription)
    {
        metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.BillingPeriodValue, subscription.BillingPeriod,
            i => i.HasValue, i => i!.Value.ToString());
        metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.BillingPeriodUnit, subscription.BillingPeriodUnit,
            unit => unit.HasValue, unit => unit!.Value.ToString());
    }

    private static void AppendPlan(this SubscriptionMetadata metadata, Subscription subscription)
    {
        if (subscription.SubscriptionItems.HasAny())
        {
            var item = subscription.SubscriptionItems.First();
            metadata[ChargebeeConstants.MetadataProperties.PlanId] = item.ItemPriceId;
        }

        metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.TrialEnd, subscription.TrialEnd,
            time => time.HasValue,
            time => time!.Value.ToIso8601());
    }

    private static void AppendInvoice(this SubscriptionMetadata metadata, Subscription subscription)
    {
        if (subscription.SubscriptionItems.HasAny())
        {
            var item = subscription.SubscriptionItems.First();
            metadata[ChargebeeConstants.MetadataProperties.BillingAmount] =
                item.Amount.GetValueOrDefault(0).ToString("G");
        }

        metadata[ChargebeeConstants.MetadataProperties.CurrencyCode] = subscription.CurrencyCode;
        metadata.TryAddIfTrue(ChargebeeConstants.MetadataProperties.NextBillingAt, subscription.NextBillingAt,
            time => time.HasValue, time => time!.Value.ToIso8601());
    }

    private static void AppendPaymentMethod(this SubscriptionMetadata metadata, Customer customer)
    {
        if (customer.PaymentMethod.Exists())
        {
            metadata[ChargebeeConstants.MetadataProperties.PaymentMethodStatus] =
                customer.PaymentMethod.Status.ToString();
            metadata[ChargebeeConstants.MetadataProperties.PaymentMethodType] =
                customer.PaymentMethod.PaymentMethodType.ToString();
        }
    }
}