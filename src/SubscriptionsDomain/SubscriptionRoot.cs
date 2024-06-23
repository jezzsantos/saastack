#region

using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;

#endregion

namespace SubscriptionsDomain;

public delegate Task<Result<SubscriptionMetadata, Error>>
    ChangePlanAction(SubscriptionRoot subscription, string planId);

public delegate Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAction(BillingProvider provider,
    Identifier transfereeId);

public delegate Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAction(SubscriptionRoot subscription);

public delegate Task<Result<SubscriptionMetadata, Error>> UnsubscribeAction(SubscriptionRoot subscription);

public delegate Task<Result<SubscriptionMetadata, Error>> ChangePaymentMethodAction(SubscriptionRoot subscription);

public delegate Task<Permission> CanChangePlanCheck(SubscriptionRoot subscription, Identifier modifierId);

public delegate Task<Permission> CanCancelSubscriptionCheck(SubscriptionRoot subscription, Identifier cancellerId);

public delegate Task<Permission> CanTransferSubscriptionCheck(SubscriptionRoot subscription, Identifier transfererId,
    Identifier transfereeId);

public delegate Task<Permission> CanViewSubscriptionCheck(SubscriptionRoot subscription, Identifier viewerId);

public delegate Task<Permission> CanUnsubscribeCheck(SubscriptionRoot subscription, Identifier unsubscriberId);

public sealed class SubscriptionRoot : AggregateRootBase
{
    public static Result<SubscriptionRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier owningEntityId, Identifier buyerId, IBillingStateInterpreter interpreter)
    {
        var root = new SubscriptionRoot(recorder, idFactory);
        root.RaiseCreateEvent(
            SubscriptionsDomain.Events.Created(root.Id, owningEntityId, buyerId, interpreter.ProviderName));
        return root;
    }

    private SubscriptionRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private SubscriptionRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    /// <summary>
    ///     The user that is the current buyer of the subscription
    /// </summary>
    public Identifier BuyerId { get; private set; } = Identifier.Empty();

    /// <summary>
    ///     The ID of the owning entity (by default, the OrganizationId)
    /// </summary>
    public Identifier OwningEntityId { get; private set; } = Identifier.Empty();

    public Optional<BillingProvider> Provider { get; private set; }

    /// <summary>
    ///     The provider specific reference to the buyer
    /// </summary>
    public Optional<string> ProviderBuyerReference { get; private set; }

    /// <summary>
    ///     The provider specific reference to the subscription
    /// </summary>
    public Optional<string> ProviderSubscriptionReference { get; private set; }

    public static AggregateRootFactory<SubscriptionRoot> Rehydrate()
    {
        return (identifier, container, _) => new SubscriptionRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (BuyerId.IsEmpty())
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_NoBuyer);
        }

        if (OwningEntityId.IsEmpty())
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_NoOwningEntity);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OwningEntityId = created.OwningEntityId.ToId();
                BuyerId = created.BuyerId.ToId();
                return Result.Ok;
            }

            case ProviderChanged changed:
            {
                var provider = BillingProvider.Create(changed.ToProviderName,
                    new SubscriptionMetadata(changed.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                ProviderBuyerReference = changed.BuyerReference;
                ProviderSubscriptionReference = changed.SubscriptionReference;
                Recorder.TraceDebug(null, "Subscription {Id} changed provider to {Provider}", Id, Provider.Value.Name);
                return Result.Ok;
            }

            case SubscriptionPlanChanged changed:
            {
                var provider = BillingProvider.Create(changed.ProviderName,
                    new SubscriptionMetadata(changed.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                ProviderBuyerReference = changed.BuyerReference;
                ProviderSubscriptionReference = changed.SubscriptionReference;
                Recorder.TraceDebug(null, "Subscription {Id} changed plan to {Plan}", Id, changed.PlanId);
                return Result.Ok;
            }

            case SubscriptionTransferred transferred:
            {
                var provider = BillingProvider.Create(transferred.ProviderName,
                    new SubscriptionMetadata(transferred.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                ProviderBuyerReference = transferred.BuyerReference;
                ProviderSubscriptionReference = transferred.SubscriptionReference;
                BuyerId = transferred.ToBuyerId.ToId();
                Recorder.TraceDebug(null,
                    "Subscription {Id} plan {Plan} was transferred from {Tranferer} to {Tranferee}", Id,
                    transferred.PlanId, transferred.FromBuyerId, transferred.ToBuyerId);
                return Result.Ok;
            }

            case SubscriptionCanceled canceled:
            {
                var provider = BillingProvider.Create(canceled.ProviderName,
                    new SubscriptionMetadata(canceled.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                Recorder.TraceDebug(null, "Subscription {Id} was canceled for {Buyer}", Id, BuyerId);
                return Result.Ok;
            }

            case SubscriptionUnsubscribed unsubscribed:
            {
                var provider = BillingProvider.Create(unsubscribed.ProviderName,
                    new SubscriptionMetadata(unsubscribed.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                Recorder.TraceDebug(null, "Subscription {Id} was unsubscribed for {Buyer}", Id, BuyerId);
                return Result.Ok;
            }

            case PaymentMethodChanged changed:
            {
                var provider = BillingProvider.Create(changed.ProviderName,
                    new SubscriptionMetadata(changed.ProviderState));
                if (provider.IsFailure)
                {
                    return provider.Error;
                }

                Provider = provider.Value;
                Recorder.TraceDebug(null, "Subscription {Id} changed its payment method for {Buyer}", Id, BuyerId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public async Task<Result<ProviderSubscription, Error>> CancelSubscriptionAsync(IBillingStateInterpreter interpreter,
        Identifier cancellerId, Roles cancellerRoles, CanCancelSubscriptionCheck canCancel,
        CancelSubscriptionAction onCancel, bool force)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var skipPermissionCheck = IsExecutedOnBehalfOfBuyer(cancellerId) || IsOperations(cancellerRoles);
        if (!skipPermissionCheck)
        {
            var canCancelPermission = await canCancel(this, cancellerId);
            if (!canCancelPermission.IsAllowed)
            {
                return canCancelPermission.ToError(Resources.SubscriptionRoot_CancelSubscription_FailedWithReason);
            }
        }

        var details = interpreter.GetSubscriptionDetails(Provider);
        if (details.IsFailure)
        {
            return details.Error;
        }

        var canceledForBuyer = await CancelSubscriptionForBuyerAsync();
        if (canceledForBuyer.IsFailure)
        {
            return canceledForBuyer.Error;
        }

        return interpreter.GetSubscriptionDetails(Provider);

        async Task<Result<Error>> CancelSubscriptionForBuyerAsync()
        {
            var isCancellable = details.Value.Status.CanBeCanceled;
            if (!isCancellable && !force)
            {
                return Error.PreconditionViolation(Resources.SubscriptionRoot_CancelSubscription_NotCancellable);
            }

            var canceled = await onCancel(this);
            if (canceled.IsFailure)
            {
                return canceled.Error;
            }

            var provider = Provider.Value.ChangeState(canceled.Value);

            return RaiseChangeEvent(
                SubscriptionsDomain.Events.SubscriptionCanceled(Id, OwningEntityId, provider));
        }
    }

    private bool IsOperations(Roles roles)
    {
        return roles.HasRole(PlatformRoles.Operations);
    }

    public async Task<Result<Error>> ChangePaymentMethodForBuyerAsync(IBillingStateInterpreter interpreter,
        Identifier modifierId, ChangePaymentMethodAction onChangePaymentMethod)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (!IsBuyer(modifierId)
            && !IsServiceAccountOrWebhookAccount(modifierId))
        {
            return Error.RoleViolation(Resources.SubscriptionRoot_ChangeBuyerPaymentMethod_NotAuthorized);
        }

        var changed = await onChangePaymentMethod(this);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var provider = Provider.Value.ChangeState(changed.Value);

        return RaiseChangeEvent(
            SubscriptionsDomain.Events.PaymentMethodChanged(Id, OwningEntityId, provider));
    }

    public async Task<Result<ProviderSubscription, Error>> ChangePlanAsync(IBillingStateInterpreter interpreter,
        Identifier modifierId, string planId, CanChangePlanCheck canChange, ChangePlanAction onChange,
        TransferSubscriptionAction onTransfer)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var skipPermissionCheck = IsExecutedOnBehalfOfBuyer(modifierId);
        if (!skipPermissionCheck)
        {
            var canChangeCheck = await canChange(this, modifierId);
            if (canChangeCheck.IsDenied)
            {
                return canChangeCheck.ToError(Resources.SubscriptionRoot_ChangePlan_FailedWithReason);
            }
        }

        var details = interpreter.GetSubscriptionDetails(Provider);
        if (details.IsFailure)
        {
            return details.Error;
        }

        if (IsBuyer(modifierId) || IsServiceAccountOrWebhookAccount(modifierId))
        {
            var changedForBuyer = await ChangePlanForBuyerAsync(interpreter, details.Value, planId, onChange);
            if (changedForBuyer.IsFailure)
            {
                return changedForBuyer.Error;
            }
        }
        else
        {
            var transferredToModifier = await TransferSubscriptionInternalAsync(interpreter, details.Value, onTransfer,
                modifierId, planId, true);
            if (transferredToModifier.IsFailure)
            {
                return transferredToModifier.Error;
            }
        }

        return interpreter.GetSubscriptionDetails(Provider);
    }

    public Result<ProviderSubscription, Error> ChangeProvider(BillingProvider provider,
        Identifier modifierId, IBillingStateInterpreter interpreter)
    {
        if (!IsServiceAccountOrWebhookAccount(modifierId))
        {
            return Error.RoleViolation(Resources.SubscriptionRoot_ChangeProvider_NotAuthorized);
        }

        var changed = ChangeProviderInternal(false, provider, interpreter);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        return interpreter.GetSubscriptionDetails(Provider.Value);
    }

    public Result<Error> DeleteSubscription(Identifier deleterId, Identifier owningEntityId)
    {
        if (!IsOwningEntityId(owningEntityId))
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_DeleteSubscription_NotOwningEntityId);
        }

        return RaisePermanentDeleteEvent(SubscriptionsDomain.Events.Deleted(Id, deleterId));
    }

    public Result<Error> SetProvider(BillingProvider provider, Identifier modifierId,
        IBillingStateInterpreter interpreter)
    {
        if (!IsBuyer(modifierId))
        {
            return Error.RoleViolation(Resources.SubscriptionRoot_NotBuyer);
        }

        return ChangeProviderInternal(true, provider, interpreter);
    }

#if TESTINGONLY
    public void TestingOnly_SetDetails(Identifier buyerId, Identifier owningEntityId)
    {
        BuyerId = buyerId;
        OwningEntityId = owningEntityId;
    }
#endif

    public async Task<Result<ProviderSubscription, Error>> TransferSubscriptionAsync(
        IBillingStateInterpreter interpreter,
        Identifier transfererId, Identifier transfereeId, CanTransferSubscriptionCheck canTransfer,
        TransferSubscriptionAction onTransfer)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (!IsBuyer(transfererId))
        {
            return Error.RoleViolation(Resources.SubscriptionRoot_NotBuyer);
        }

        var canTransferCheck = await canTransfer(this, transfererId, transfereeId);
        if (canTransferCheck.IsDenied)
        {
            return canTransferCheck.ToError(Resources.SubscriptionRoot_TransferSubscription_FailedWithReason);
        }

        var details = interpreter.GetSubscriptionDetails(Provider);
        if (details.IsFailure)
        {
            return details.Error;
        }

        var planId = details.Value.Plan.PlanId;
        var transferredToModifier = await TransferSubscriptionInternalAsync(interpreter, details.Value, onTransfer,
            transfereeId, planId, false);
        if (transferredToModifier.IsFailure)
        {
            return transferredToModifier.Error;
        }

        return interpreter.GetSubscriptionDetails(Provider);
    }

    public async Task<Result<Error>> UnsubscribeSubscriptionAsync(IBillingStateInterpreter interpreter,
        Identifier unsubscriberId, bool skipChecks, CanUnsubscribeCheck canUnsubscribe,
        UnsubscribeAction onUnsubscribe)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (!skipChecks)
        {
            var skipPermissionCheck = IsExecutedOnBehalfOfBuyer(unsubscriberId);
            if (!skipPermissionCheck)
            {
                var canUnsubscribeCheck = await canUnsubscribe(this, unsubscriberId);
                if (!canUnsubscribeCheck.IsAllowed)
                {
                    return canUnsubscribeCheck.ToError(Resources
                        .SubscriptionRoot_UnsubscribeSubscription_FailedWithReason);
                }
            }

            var providerSubscription = interpreter.GetSubscriptionDetails(Provider);
            if (providerSubscription.IsFailure)
            {
                return providerSubscription.Error;
            }

            var canBeUnsubscribed = providerSubscription.Value.Status.CanBeUnsubscribed;
            if (!canBeUnsubscribed)
            {
                return Error.RuleViolation(Resources.SubscriptionRoot_CannotBeUnsubscribed);
            }
        }

        var unsubscribed = await onUnsubscribe(this);
        if (unsubscribed.IsFailure)
        {
            return unsubscribed.Error;
        }

        var provider = Provider.Value.ChangeState(unsubscribed.Value);

        return RaiseChangeEvent(
            SubscriptionsDomain.Events.SubscriptionUnsubscribed(Id, OwningEntityId, provider));
    }

    public async Task<Result<ProviderSubscription, Error>> ViewSubscriptionAsync(IBillingStateInterpreter interpreter,
        Identifier viewerId, CanViewSubscriptionCheck canView)
    {
        var skipPermissionCheck = IsExecutedOnBehalfOfBuyer(viewerId);
        if (!skipPermissionCheck)
        {
            var canViewPermission = await canView(this, viewerId);
            if (!canViewPermission.IsAllowed && !IsServiceAccount(viewerId))
            {
                return Error.RoleViolation(
                    Resources.SubscriptionRoot_ViewSubscription_FailedWithReason.Format(canViewPermission
                        .DisallowedReason));
            }
        }

        var providerSubscription = interpreter.GetSubscriptionDetails(Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return providerSubscription.Value;
    }

    private static bool IsServiceAccount(Identifier userId)
    {
        return CallerConstants.IsServiceAccount(userId);
    }

    private async Task<Result<Error>> ChangePlanForBuyerAsync(IBillingStateInterpreter interpreter,
        ProviderSubscription providerSubscription, string planId, ChangePlanAction onChange)
    {
        var paymentMethod = providerSubscription.PaymentMethod;
        if (paymentMethod.Status != BillingPaymentMethodStatus.Valid)
        {
            return Error.FeatureViolation(
                Resources.SubscriptionRoot_ChangePlan_InvalidPaymentMethod);
        }

        var changed = await onChange(this, planId);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var provider = Provider.Value.ChangeState(changed.Value);

        var buyerReference = interpreter.GetBuyerReference(provider);
        if (buyerReference.IsFailure)
        {
            return buyerReference.Error;
        }

        var subscriptionReference = interpreter.GetSubscriptionReference(provider);
        if (subscriptionReference.IsFailure)
        {
            return subscriptionReference.Error;
        }

        return RaiseChangeEvent(SubscriptionsDomain.Events.SubscriptionPlanChanged(Id, OwningEntityId, planId,
            provider, buyerReference.Value, subscriptionReference.Value));
    }

    /// <summary>
    ///     Either the buyer can make the transfer to another billing admin,
    ///     or a billingAdmin can force it, but only if the subscription is already canceled/unsubscribed
    /// </summary>
    private async Task<Result<Error>> TransferSubscriptionInternalAsync(IBillingStateInterpreter interpreter,
        ProviderSubscription providerSubscription, TransferSubscriptionAction onTransfer, Identifier transfereeId,
        string planId, bool checkTransferByBillingAdmin)
    {
        var paymentMethod = providerSubscription.PaymentMethod;
        if (paymentMethod.Status != BillingPaymentMethodStatus.Valid)
        {
            return Error.FeatureViolation(Resources.SubscriptionRoot_TransferSubscription_InvalidPaymentMethod);
        }

        if (checkTransferByBillingAdmin)
        {
            var status = providerSubscription.Status.Status;
            if (status != BillingSubscriptionStatus.Canceled
                && status != BillingSubscriptionStatus.Unsubscribed)
            {
                return Error.RuleViolation(Resources.SubscriptionRoot_ChangePlan_NotClaimable);
            }
        }

        var transferred = await onTransfer(Provider, transfereeId);
        if (transferred.IsFailure)
        {
            return transferred.Error;
        }

        var provider = Provider.Value.ChangeState(transferred.Value);

        var buyerReference = interpreter.GetBuyerReference(provider);
        if (buyerReference.IsFailure)
        {
            return buyerReference.Error;
        }

        var subscriptionReference = interpreter.GetSubscriptionReference(provider);
        if (subscriptionReference.IsFailure)
        {
            return subscriptionReference.Error;
        }

        return RaiseChangeEvent(SubscriptionsDomain.Events.SubscriptionTransferred(Id, OwningEntityId,
            BuyerId, transfereeId, planId, provider, buyerReference.Value,
            subscriptionReference.Value));
    }

    private static bool IsExecutedOnBehalfOfBuyer(Identifier userId)
    {
        return IsServiceAccountOrWebhookAccount(userId);
    }

    private Result<Error> ChangeProviderInternal(bool isRecentlySubscribed, BillingProvider provider,
        IBillingStateInterpreter interpreter)
    {
        var verified = VerifyProviderIsChangeable(interpreter, provider);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var translatedProvider = isRecentlySubscribed
            ? interpreter.SetInitialProviderState(provider)
            : new Result<BillingProvider, Error>(provider);
        if (translatedProvider.IsFailure)
        {
            return translatedProvider.Error;
        }

        var buyerReference = interpreter.GetBuyerReference(translatedProvider.Value);
        if (buyerReference.IsFailure)
        {
            return buyerReference.Error;
        }

        var subscriptionReference = interpreter.GetSubscriptionReference(translatedProvider.Value);
        if (subscriptionReference.IsFailure)
        {
            return subscriptionReference.Error;
        }

        var fromProviderName = Provider.HasValue
            ? Provider.Value.Name
            : null;
        return RaiseChangeEvent(SubscriptionsDomain.Events.ProviderChanged(Id, OwningEntityId, fromProviderName,
            translatedProvider.Value,
            buyerReference.Value, subscriptionReference.Value));
    }

    private Result<Error> VerifyProviderIsSameAsInstalled(IBillingStateInterpreter interpreter)
    {
        if (!Provider.HasValue)
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_NoProvider);
        }

        if (!Provider.Value.IsInitialized)
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_ProviderNotInitialized);
        }

        var installedProviderName = interpreter.ProviderName;
        if (!Provider.Value.IsCurrentProvider(installedProviderName))
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_ProviderMismatch);
        }

        return Result.Ok;
    }

    private Result<Error> VerifyProviderIsChangeable(IBillingStateInterpreter interpreter, BillingProvider provider)
    {
        if (Provider.HasValue)
        {
            if (Provider.Value.IsCurrentProvider(provider.Name))
            {
                return Error.RuleViolation(Resources.SubscriptionRoot_SameProvider);
            }
        }

        var installedProviderName = interpreter.ProviderName;
        if (!provider.IsCurrentProvider(installedProviderName))
        {
            return Error.RuleViolation(Resources.SubscriptionRoot_ProviderMismatch);
        }

        return Result.Ok;
    }

    private bool IsBuyer(Identifier userId)
    {
        return BuyerId == userId;
    }

    /// <summary>
    ///     Whether the user is a maintenance account or a webhook account.
    ///     Webhook accounts will update the subscriptions.
    ///     Maintenance accounts for incoming events.
    /// </summary>
    private static bool IsServiceAccountOrWebhookAccount(Identifier userId)
    {
        return userId == CallerConstants.MaintenanceAccountUserId
               || userId == CallerConstants.ExternalWebhookAccountUserId;
    }

    private bool IsOwningEntityId(Identifier entityId)
    {
        return OwningEntityId == entityId;
    }
}