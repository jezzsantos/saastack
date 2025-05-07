using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using Subscription = SubscriptionsApplication.Persistence.ReadModels.Subscription;
using Validations = SubscriptionsDomain.Validations;

namespace SubscriptionsApplication;

public partial class SubscriptionsApplication : ISubscriptionsApplication
{
    private readonly IBillingProvider _billingProvider;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly ISubscriptionRepository _repository;
    private readonly ISubscriptionOwningEntityService _subscriptionOwningEntityService;
    private readonly IUserProfilesService _userProfilesService;

    public SubscriptionsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IUserProfilesService userProfilesService, IBillingProvider billingProvider,
        ISubscriptionOwningEntityService subscriptionOwningEntityService, ISubscriptionRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _userProfilesService = userProfilesService;
        _billingProvider = billingProvider;
        _subscriptionOwningEntityService = subscriptionOwningEntityService;
        _repository = repository;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await CancelSubscriptionInternalAsync(caller, subscription, CancelSubscriptionOptions.EndOfTerm,
            cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> ChangePlanAsync(ICallerContext caller, string owningEntityId,
        string planId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await ChangeSubscriptionPlanInternalAsync(caller, subscription, planId, cancellationToken);
    }

    public async Task<Result<SearchResults<SubscriptionToMigrate>, Error>> ExportSubscriptionsToMigrateAsync(
        ICallerContext caller, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var searched =
            await _repository.SearchAllByProviderAsync(_billingProvider.StateInterpreter.ProviderName,
                searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var subscriptions = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All subscriptions were fetched");

        return subscriptions.ToSearchResults(searchOptions, subscription =>
        {
            var buyer = CreateBuyerAsync(caller, subscription.BuyerId.Value.ToId(),
                subscription.OwningEntityId.Value.ToId(), cancellationToken).GetAwaiter().GetResult();
            if (buyer.IsFailure)
            {
                return subscription.ToSubscriptionForMigration(null);
            }

            return subscription.ToSubscriptionForMigration(buyer.Value);
        });
    }

    public async Task<Result<SubscriptionWithPlan, Error>> ForceCancelSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await CancelSubscriptionInternalAsync(caller, subscription, CancelSubscriptionOptions.Immediately,
            cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await GetSubscriptionInternalAsync(caller, subscription, cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionPrivateAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await GetSubscriptionInternalAsync(caller, subscription, cancellationToken);
    }

    public async Task<Result<PricingPlans, Error>> ListPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var plans = await _billingProvider.GatewayService.ListAllPricingPlansAsync(caller, cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all pricing plans");

        return plans;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> MigrateSubscriptionAsync(ICallerContext caller,
        string? owningEntityId, string providerName, Dictionary<string, string> providerState,
        CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var provider = BillingProvider.Create(providerName, new SubscriptionMetadata(providerState));
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var providerBefore = subscription.Provider.Value.Name;
        var modifierId = caller.ToCallerId();
        var changed = subscription.ChangeProvider(provider.Value, modifierId, _billingProvider.StateInterpreter);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var providerAfter = subscription.Provider.Value.Name;
        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} has changed its provider from: {ProviderBefore}, to: {ProviderAfter}", subscription.Id,
            providerBefore, providerAfter);

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);
    }

    public async Task<Result<SearchResults<Invoice>, Error>> SearchSubscriptionHistoryAsync(ICallerContext caller,
        string owningEntityId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var (from, to) = CalculatedSearchRange(fromUtc, toUtc);
        var searched = await _billingProvider.GatewayService.SearchAllInvoicesAsync(caller, subscription.Provider,
            from, to, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved subscription invoices for: {OwningEntity}",
            owningEntityId);

        return searched;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> TransferSubscriptionAsync(ICallerContext caller,
        string owningEntityId, string billingAdminId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var transfererId = caller.ToCallerId();
        var transfereeId = billingAdminId.ToId();
        var beforeBuyerId = subscription.BuyerId;
        var transferred = await subscription.TransferSubscriptionAsync(_billingProvider.StateInterpreter, transfererId,
            transfereeId, CanTransfer, OnTransfer);
        if (transferred.IsFailure)
        {
            return transferred.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var afterBuyerId = subscription.BuyerId;
        _recorder.TraceInformation(caller.ToCall(),
            "Transferred subscription: {Id} for entity: {OwningEntity}, from {BeforeBuyer}, to: {AfterBuyer}",
            subscription.Id, subscription.OwningEntityId, beforeBuyerId, afterBuyerId);
        _recorder.AuditAgainst(caller.ToCall(), transfererId, Audits.SubscriptionsApplication_BuyerTransferred,
            "EndUser {TransfererId} transferred subscription {Id} to: {Buyer}", transfererId, subscription.Id,
            afterBuyerId);

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Permission> CanTransfer(SubscriptionRoot subscription1, Identifier transfererId1,
            Identifier transfereeId1)
        {
            return (await _subscriptionOwningEntityService.CanTransferSubscriptionAsync(caller,
                    subscription1.OwningEntityId,
                    transfererId1, transfereeId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }

        Task<Result<SubscriptionMetadata, Error>> OnTransfer(BillingProvider provider, Identifier transfereeId1)
        {
            var maintenanceCaller = Caller.CreateAsMaintenance(caller.CallId);
            var transferee = CreateBuyerAsync(maintenanceCaller, transfereeId1, owningEntityId.ToId(),
                cancellationToken);
            if (transferee.Result.IsFailure)
            {
                return Task.FromResult<Result<SubscriptionMetadata, Error>>(transferee.Result.Error);
            }

            return _billingProvider.GatewayService.TransferSubscriptionAsync(caller, new TransferSubscriptionOptions
            {
                TransfereeBuyer = transferee.Result.Value
            }, provider, cancellationToken);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> ChangeSubscriptionPlanInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, string planId, CancellationToken cancellationToken)
    {
        var buyerIdBeforeChange = subscription.BuyerId;
        var modifierId = caller.ToCallerId();
        var changed = await subscription.ChangePlanAsync(_billingProvider.StateInterpreter, modifierId, planId,
            CanChange, OnChange, OnTransfer);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var buyerIdAfterChange = subscription.BuyerId;
        if (buyerIdAfterChange != buyerIdBeforeChange)
        {
            _recorder.TraceInformation(caller.ToCall(),
                "Subscription {Id} has been transferred from {FromBuyer} to {ToBuyer}", subscription.Id,
                buyerIdBeforeChange, buyerIdAfterChange);
        }
        else
        {
            _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} changed its plan: {Plan}",
                subscription.Id, planId);
        }

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        Task<Result<SubscriptionMetadata, Error>> OnChange(SubscriptionRoot subscription1, string planId1)
        {
            var options = new ChangePlanOptions
            {
                PlanId = planId1,
                Subscriber = new Subscriber
                {
                    EntityId = subscription.OwningEntityId.ToString(),
                    EntityType = nameof(Organization)
                }
            };

            var planChanged = _billingProvider.GatewayService.ChangeSubscriptionPlanAsync(caller, options,
                subscription1.Provider.Value, cancellationToken);
            return Task.FromResult(planChanged.Result);
        }

        async Task<Permission> CanChange(SubscriptionRoot subscription1, Identifier modifierId1)
        {
            return (await _subscriptionOwningEntityService.CanChangeSubscriptionPlanAsync(caller,
                    subscription1.OwningEntityId,
                    modifierId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }

        Task<Result<SubscriptionMetadata, Error>> OnTransfer(BillingProvider provider, Identifier transfereeId)
        {
            var transferee = CreateBuyerAsync(caller, transfereeId, subscription.OwningEntityId,
                cancellationToken);
            if (transferee.Result.IsFailure)
            {
                return Task.FromResult<Result<SubscriptionMetadata, Error>>(transferee.Result.Error);
            }

            return _billingProvider.GatewayService.TransferSubscriptionAsync(caller, new TransferSubscriptionOptions
            {
                TransfereeBuyer = transferee.Result.Value,
                PlanId = planId
            }, provider, cancellationToken);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, CancelSubscriptionOptions options, CancellationToken cancellationToken)
    {
        var cancellerId = caller.ToCallerId();
        var cancellerRoles = Roles.Create(caller.Roles.All);
        if (cancellerRoles.IsFailure)
        {
            return cancellerRoles.Error;
        }

        var canceled =
            await subscription.CancelSubscriptionAsync(_billingProvider.StateInterpreter, cancellerId,
                cancellerRoles.Value, CanCancel, OnCancel, false);
        if (canceled.IsFailure)
        {
            return canceled.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Canceled subscription: {Id} for entity: {OwningEntity}",
            subscription.Id, subscription.OwningEntityId);

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        Task<Result<SubscriptionMetadata, Error>> OnCancel(SubscriptionRoot subscription1)
        {
            var canceledSubscription = _billingProvider.GatewayService.CancelSubscriptionAsync(caller,
                options, subscription1.Provider.Value, cancellationToken);
            return Task.FromResult(canceledSubscription.Result);
        }

        async Task<Permission> CanCancel(SubscriptionRoot subscription1, Identifier cancellerId1)
        {
            return (await _subscriptionOwningEntityService.CanCancelSubscriptionAsync(caller,
                    subscription1.OwningEntityId, cancellerId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, CancellationToken cancellationToken)
    {
        var viewerId = caller.ToCallerId();
        var providerSubscription = await subscription.ViewSubscriptionAsync(_billingProvider.StateInterpreter, viewerId,
            CanView);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved subscription: {Id} for entity: {OwningEntity}",
            subscription.Id, subscription.OwningEntityId);
        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Permission> CanView(SubscriptionRoot subscription1, Identifier viewerId1)
        {
            return (await _subscriptionOwningEntityService.CanViewSubscriptionAsync(caller,
                    subscription1.OwningEntityId,
                    viewerId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }
    }

    /// <summary>
    ///     Calculate the date range based on inputs and defaults
    ///     Note: If not explicitly specified, the range should be
    ///     <see cref="SubscriptionsDomain.Validations.Subscription.DefaultInvoicePeriod" />
    ///     in length, and as muh as possible include those past months
    /// </summary>
    private static (DateTime From, DateTime To) CalculatedSearchRange(DateTime? fromUtc, DateTime? toUtc)
    {
        var to = toUtc ?? (fromUtc?.Add(Validations.Subscription.DefaultInvoicePeriod)
                           ?? DateTime.UtcNow.ToNearestMinute());
        var from = fromUtc ?? to.Add(-Validations.Subscription.DefaultInvoicePeriod);

        return (from, to);
    }

    private async Task<Result<SubscriptionRoot, Error>> GetSubscriptionByOwningEntityAsync(Identifier owningEntityId,
        CancellationToken cancellationToken)
    {
        var retrievedSubscription =
            await _repository.FindByOwningEntityIdAsync(owningEntityId, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        return retrievedSubscription.Value.Value;
    }

    private async Task<Result<Optional<SubscriptionBuyer>, Error>> CreateBuyerAsync(ICallerContext caller,
        Identifier buyerId, Optional<Identifier> owningEntityId, CancellationToken cancellationToken)
    {
        var retrievedProfile = await _userProfilesService.GetProfilePrivateAsync(caller, buyerId, cancellationToken);
        if (retrievedProfile.IsFailure)
        {
            if (retrievedProfile.Error.Is(ErrorCode.EntityNotFound))
            {
                return Optional<SubscriptionBuyer>.None;
            }

            return retrievedProfile.Error;
        }

        var profile = retrievedProfile.Value;
        var buyer = ToSubscriptionBuyer(buyerId, owningEntityId, profile);

        return buyer.ToOptional();
    }

    private static SubscriptionBuyer ToSubscriptionBuyer(Identifier buyerId,
        Identifier owningEntityId,
        UserProfile profile)
    {
        return new SubscriptionBuyer
        {
            Id = buyerId.ToString(),
            Name = profile.Name,
            PhoneNumber = profile.PhoneNumber,
            EmailAddress = profile.EmailAddress!,
            Address = profile.Address,
            Subscriber = new Subscriber
            {
                EntityId = owningEntityId,
                EntityType = nameof(Organization)
            }
        };
    }
}

internal static class SubscriptionConversionExtensions
{
    public static SubscriptionWithPlan ToSubscription(this SubscriptionRoot subscription,
        ProviderSubscription providerSubscription)
    {
        return new SubscriptionWithPlan
        {
            Id = subscription.Id,
            OwningEntityId = subscription.OwningEntityId,
            BuyerId = subscription.BuyerId,
            ProviderName = subscription.Provider.HasValue
                ? subscription.Provider.Value.Name
                : null,
            ProviderState = subscription.Provider.HasValue
                ? subscription.Provider.Value.State
                : new Dictionary<string, string>(),
            SubscriptionReference = providerSubscription.SubscriptionReference.HasValue
                ? providerSubscription.SubscriptionReference.Value.ToString()
                : null,
            BuyerReference = subscription.ProviderBuyerReference.ValueOrDefault!,
            Status = providerSubscription.Status.Status.ToEnumOrDefault(SubscriptionStatus.Unsubscribed),
            CanceledDateUtc = providerSubscription.Status.CanceledDateUtc.HasValue
                ? providerSubscription.Status.CanceledDateUtc.Value
                : null,
            Plan = new SubscriptionPlan
            {
                Id = providerSubscription.Plan.PlanId.ValueOrDefault,
                IsTrial = providerSubscription.Plan.IsTrial,
                TrialEndDateUtc = providerSubscription.Plan.TrialEndDateUtc.HasValue
                    ? providerSubscription.Plan.TrialEndDateUtc.Value
                    : null,
                Tier = providerSubscription.Plan.Tier.ToEnum<BillingSubscriptionTier, SubscriptionTier>()
            },
            Period = new PlanPeriod
            {
                Frequency = providerSubscription.Period.Frequency,
                Unit = providerSubscription.Period.Unit.ToEnumOrDefault(PeriodFrequencyUnit.Eternity)
            },
            Invoice = new InvoiceSummary
            {
                Amount = providerSubscription.Invoice.Amount,
                Currency = providerSubscription.Invoice.CurrencyCode.Currency.Code,
                NextUtc = providerSubscription.Invoice.NextUtc.HasValue
                    ? providerSubscription.Invoice.NextUtc.Value
                    : null
            },
            PaymentMethod = new SubscriptionPaymentMethod
            {
                Status = providerSubscription.PaymentMethod.Status.ToEnumOrDefault(PaymentMethodStatus.Invalid),
                Type = providerSubscription.PaymentMethod.Type.ToEnumOrDefault(PaymentMethodType.None),
                ExpiresOn = providerSubscription.PaymentMethod.ExpiresOn.HasValue
                    ? providerSubscription.PaymentMethod.ExpiresOn.Value
                    : null
            },
            CanBeUnsubscribed = providerSubscription.Status.CanBeUnsubscribed,
            CanBeCanceled = providerSubscription.Status.CanBeCanceled
        };
    }

    public static SubscriptionToMigrate ToSubscriptionForMigration(this Subscription subscription,
        SubscriptionBuyer? buyer)
    {
        var dto = new SubscriptionToMigrate
        {
            Id = subscription.Id,
            BuyerId = subscription.BuyerId,
            OwningEntityId = subscription.OwningEntityId,
            ProviderName = subscription.ProviderName,
            ProviderState = subscription.ProviderState.Value.FromJson<Dictionary<string, string>>()!,
            Buyer = buyer.Exists()
                ? new Dictionary<string, string>(buyer.ToStringDictionary())
                : new Dictionary<string, string>()
        };

        return dto;
    }
}