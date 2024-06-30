using ChargeBee.Models;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides an interpreter for managing the subscription state of a Chargebee subscription.
/// </summary>
public sealed class ChargebeeStateInterpreter : IBillingStateInterpreter
{
    private const string Tier1PlanIdsSettingName = "ApplicationServices:Chargebee:Plans:Tier1PlanIds";
    private const string Tier2PlanIdsSettingName = "ApplicationServices:Chargebee:Plans:Tier2PlanIds";
    private const string Tier3PlanIdsSettingName = "ApplicationServices:Chargebee:Plans:Tier3PlanIds";
    private static readonly char[] TierPlanIdsDelimiters = [',', ';'];
    private readonly string _tier1PlanIds;
    private readonly string _tier2PlanIds;
    private readonly string _tier3PlanIds;

    public ChargebeeStateInterpreter(IConfigurationSettings settings) : this(
        settings.Platform.GetString(Tier1PlanIdsSettingName, string.Empty),
        settings.Platform.GetString(Tier2PlanIdsSettingName, string.Empty),
        settings.Platform.GetString(Tier3PlanIdsSettingName, string.Empty))
    {
    }

    internal ChargebeeStateInterpreter(string tier1PlanIds) : this(tier1PlanIds, string.Empty, string.Empty)
    {
    }

    private ChargebeeStateInterpreter(string tier1PlanIds, string tier2PlanIds, string tier3PlanIds)
    {
        _tier1PlanIds = tier1PlanIds;
        _tier2PlanIds = tier2PlanIds;
        _tier3PlanIds = tier3PlanIds;
    }

    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        if (current.State.TryGetValue(Constants.MetadataProperties.CustomerId, out var customerId))
        {
            return customerId;
        }

        return Error.RuleViolation(
            Resources.BillingProvider_PropertyNotFound.Format(Constants.MetadataProperties.CustomerId,
                GetType().FullName!));
    }

    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        var paymentMethod = current.State.ToPaymentMethod();
        if (paymentMethod.IsFailure)
        {
            return paymentMethod.Error;
        }

        if (!current.State.TryGetValue(Constants.MetadataProperties.SubscriptionId, out var subscriptionId))
        {
            return ProviderSubscription.Create(ProviderStatus.Empty, paymentMethod.Value);
        }

        var status = current.State.ToStatus();
        if (status.IsFailure)
        {
            return status.Error;
        }

        var planMap = CreatePlanTierMap(_tier1PlanIds, _tier2PlanIds, _tier3PlanIds);
        var plan = current.State.ToPlan(status.Value.Status, planMap);
        if (plan.IsFailure)
        {
            return plan.Error;
        }

        var period = current.State.ToPlanPeriod();
        if (period.IsFailure)
        {
            return period.Error;
        }

        var invoice = current.State.ToInvoice();
        if (invoice.IsFailure)
        {
            return invoice.Error;
        }

        return ProviderSubscription.Create(subscriptionId.ToId(), status.Value, plan.Value, period.Value, invoice.Value,
            paymentMethod.Value);
    }

    public Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current)
    {
        if (current.State.TryGetValue(Constants.MetadataProperties.SubscriptionId, out var subscriptionId))
        {
            return subscriptionId.ToOptional();
        }

        return Optional<string>.None;
    }

    public string ProviderName => Constants.ProviderName;

    public Result<BillingProvider, Error> SetInitialProviderState(BillingProvider provider)
    {
        if (provider.Name.IsInvalidParameter(name => name.EqualsIgnoreCase(Constants.ProviderName),
                nameof(provider.Name), Resources.BillingProvider_ProviderNameNotMatch,
                out var error1))
        {
            return error1;
        }

        if (!provider.State.TryGetValue(Constants.MetadataProperties.SubscriptionId, out _))
        {
            return Error.RuleViolation(
                Resources.BillingProvider_PropertyNotFound.Format(Constants.MetadataProperties.SubscriptionId,
                    GetType().FullName!));
        }

        if (!provider.State.TryGetValue(Constants.MetadataProperties.CustomerId, out _))
        {
            return Error.RuleViolation(
                Resources.BillingProvider_PropertyNotFound.Format(Constants.MetadataProperties.CustomerId,
                    GetType().FullName!));
        }

        return provider;
    }

    private static Dictionary<string, BillingSubscriptionTier> CreatePlanTierMap(string tier1PlanIds,
        string tier2PlanIds, string tier3PlanIds)
    {
        var map = new Dictionary<string, BillingSubscriptionTier>();
        AddTierPlans(BillingSubscriptionTier.Standard, tier1PlanIds);
        AddTierPlans(BillingSubscriptionTier.Professional, tier2PlanIds);
        AddTierPlans(BillingSubscriptionTier.Enterprise, tier3PlanIds);
        return map;

        void AddTierPlans(BillingSubscriptionTier tier, string planIds)
        {
            var planIdsList = planIds.Split(TierPlanIdsDelimiters,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var planId in planIdsList)
            {
                map[planId] = tier;
            }
        }
    }

    public static class Constants
    {
        public const string ProviderName = "chargebee_billing";
#if TESTINGONLY
        public static readonly IChargebeeClient.CreditCardPaymentSource TestCard = new()
        {
            Number = "4111111111111111",
            Cvv = "100",
            ExpiryYear = DateTime.UtcNow.Year + 2,
            ExpiryMonth = 12
        };
#endif

        public static class MetadataProperties
        {
            public const string BillingAmount = "BillingAmount";
            public const string BillingPeriodUnit = "BillingPeriodUnit";
            public const string BillingPeriodValue = "BillingPeriodValue";
            public const string CanceledAt = "CanceledAt";
            public const string CurrencyCode = "CurrencyCode";
            public const string CustomerId = "CustomerId";
            public const string NextBillingAt = "NextBillingAt";
            public const string PaymentMethodStatus = "PaymentMethodStatus";
            public const string PaymentMethodType = "PaymentMethodType";
            public const string PlanId = "PlanId";
            public const string SubscriptionDeleted = "SubscriptionDeleted";
            public const string SubscriptionId = "SubscriptionId";
            public const string SubscriptionStatus = "SubscriptionStatus";
            public const string TrialEnd = "TrialEnd";
        }
    }
}

internal static class ChargebeeInterpreterConversionExtensions
{
    public static Result<ProviderInvoice, Error> ToInvoice(this SubscriptionMetadata state)
    {
        var currencyCode =
            state.GetValueOrDefault(ChargebeeStateInterpreter.Constants.MetadataProperties.CurrencyCode,
                CurrencyCodes.Default.Code);
        var amount =
            state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.BillingAmount, out var value)
                ? CurrencyCodes.FromMinorUnit(currencyCode,
                    value.ToIntOrDefault(0))
                : 0M;
        var nextUtc =
            state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.NextBillingAt, out var value2)
                ? value2.ToLongOrDefault(0).FromUnixTimestamp().ToOptional()
                : Optional<DateTime>.None;

        return ProviderInvoice.Create(amount, currencyCode, nextUtc);
    }

    public static Result<ProviderPaymentMethod, Error> ToPaymentMethod(this SubscriptionMetadata state)
    {
        var paymentStatus = state.TryGetValue(
            ChargebeeStateInterpreter.Constants.MetadataProperties.PaymentMethodStatus,
            out var value2)
            ? value2.ToPaymentMethodStatus()
            : BillingPaymentMethodStatus.Invalid;

        if (paymentStatus == BillingPaymentMethodStatus.Invalid)
        {
            return ProviderPaymentMethod.Empty;
        }

        var paymentType = state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.PaymentMethodType,
            out var value)
            ? value.ToPaymentMethodType()
            : BillingPaymentMethodType.None;
        return ProviderPaymentMethod.Create(paymentType, paymentStatus, Optional<DateOnly>.None);
    }

    public static Result<ProviderPlan, Error> ToPlan(this SubscriptionMetadata state,
        BillingSubscriptionStatus status, Dictionary<string, BillingSubscriptionTier> planMap)
    {
        if (!state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.PlanId, out var planId))
        {
            return ProviderPlan.Empty;
        }

        var isInTrial = IsInTrial(state);
        var trialEndDate = state.ToTrialEndDate();
        var tier = status.ToTier(planId, planMap);

        return ProviderPlan.Create(planId.ToId(), isInTrial, trialEndDate, tier);
    }

    public static Result<ProviderPlanPeriod, Error> ToPlanPeriod(this SubscriptionMetadata state)
    {
        var frequency = state
            .GetValueOrDefault(ChargebeeStateInterpreter.Constants.MetadataProperties.BillingPeriodValue, "0")
            .ToIntOrDefault(0);

        if (!state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.BillingPeriodUnit,
                out var periodUnit))
        {
            return ProviderPlanPeriod.Create(frequency, BillingFrequencyUnit.Eternity);
        }

        if (periodUnit.HasNoValue())
        {
            return ProviderPlanPeriod.Create(frequency, BillingFrequencyUnit.Eternity);
        }

        var unit = periodUnit.ToBillingUnit();
        return ProviderPlanPeriod.Create(frequency, unit);
    }

    public static Result<ProviderStatus, Error> ToStatus(this SubscriptionMetadata state)
    {
        var subscriptionStatus = state.ToSubscriptionStatus();
        var canBeUnsubscribed = state.ToCanBeUnsubscribed(subscriptionStatus);
        return ProviderStatus.Create(subscriptionStatus, state.ToCanceledDate(), canBeUnsubscribed);
    }

    private static BillingFrequencyUnit ToBillingUnit(this string value)
    {
        if (value.HasNoValue())
        {
            return BillingFrequencyUnit.Eternity;
        }

        if (Enum.TryParse(typeof(Subscription.BillingPeriodUnitEnum), value, true, out var unit))
        {
            return unit switch
            {
                Subscription.BillingPeriodUnitEnum.Day => BillingFrequencyUnit.Day,
                Subscription.BillingPeriodUnitEnum.Week => BillingFrequencyUnit.Week,
                Subscription.BillingPeriodUnitEnum.Month => BillingFrequencyUnit.Month,
                Subscription.BillingPeriodUnitEnum.Year => BillingFrequencyUnit.Year,
                _ => BillingFrequencyUnit.Eternity
            };
        }

        return BillingFrequencyUnit.Eternity;
    }

    private static BillingPaymentMethodStatus ToPaymentMethodStatus(this string value)
    {
        if (value.HasNoValue())
        {
            return BillingPaymentMethodStatus.Invalid;
        }

        if (Enum.TryParse(typeof(Customer.CustomerPaymentMethod.StatusEnum), value, true, out var status))
        {
            return status switch
            {
                Customer.CustomerPaymentMethod.StatusEnum.Valid => BillingPaymentMethodStatus.Valid,
                _ => BillingPaymentMethodStatus.Invalid
            };
        }

        return BillingPaymentMethodStatus.Invalid;
    }

    private static BillingPaymentMethodType ToPaymentMethodType(this string value)
    {
        if (value.HasNoValue())
        {
            return BillingPaymentMethodType.Other;
        }

        if (Enum.TryParse(typeof(Customer.CustomerPaymentMethod.TypeEnum), value, true, out var type))
        {
            return type switch
            {
                Customer.CustomerPaymentMethod.TypeEnum.Card => BillingPaymentMethodType.Card,
                _ => BillingPaymentMethodType.Other
            };
        }

        return BillingPaymentMethodType.Other;
    }

    private static BillingSubscriptionStatus ToSubscriptionStatus(this string value)
    {
        if (value.HasNoValue())
        {
            return BillingSubscriptionStatus.Unsubscribed;
        }

        if (Enum.TryParse(typeof(Subscription.StatusEnum), value, true, out var status))
        {
            return status switch
            {
                Subscription.StatusEnum.Future
                    or Subscription.StatusEnum.InTrial
                    or Subscription.StatusEnum.Active
                    or Subscription.StatusEnum.Paused =>
                    BillingSubscriptionStatus.Activated,
                Subscription.StatusEnum.NonRenewing => BillingSubscriptionStatus.Canceling,
                Subscription.StatusEnum.Cancelled => BillingSubscriptionStatus.Canceled,
                _ => BillingSubscriptionStatus.Unsubscribed
            };
        }

        return BillingSubscriptionStatus.Unsubscribed;
    }

    private static BillingSubscriptionTier ToTier(this BillingSubscriptionStatus status, string planId,
        Dictionary<string, BillingSubscriptionTier> planMap)
    {
        if (status != BillingSubscriptionStatus.Activated
            && status != BillingSubscriptionStatus.Canceling)
        {
            return BillingSubscriptionTier.Unsubscribed;
        }

        return planMap.GetValueOrDefault(planId, BillingSubscriptionTier.Unsubscribed);
    }

    private static Optional<DateTime> ToTrialEndDate(this SubscriptionMetadata state)
    {
        if (!state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.TrialEnd, out var trialEnd))
        {
            return Optional<DateTime>.None;
        }

        if (trialEnd.HasNoValue())
        {
            return Optional<DateTime>.None;
        }

        var seconds = trialEnd.ToLongOrDefault(0);
        return seconds > 0
            ? seconds.FromUnixTimestamp()
            : Optional<DateTime>.None;
    }

    private static BillingSubscriptionStatus ToSubscriptionStatus(this SubscriptionMetadata state)
    {
        if (state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.SubscriptionDeleted,
                out var deleted))
        {
            if (deleted.HasValue() && deleted.ToBool())
            {
                return BillingSubscriptionStatus.Unsubscribed;
            }
        }

        return state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.SubscriptionStatus,
            out var value)
            ? value.ToSubscriptionStatus()
            : BillingSubscriptionStatus.Unsubscribed;
    }

    private static Optional<DateTime> ToCanceledDate(this SubscriptionMetadata state)
    {
        var canceledSecs = state.GetValueOrDefault(ChargebeeStateInterpreter.Constants.MetadataProperties.CanceledAt)
            .ToLongOrDefault(0);
        if (canceledSecs > 0)
        {
            return canceledSecs.FromUnixTimestamp();
        }

        return Optional<DateTime>.None;
    }

    private static bool ToCanBeUnsubscribed(this SubscriptionMetadata state, BillingSubscriptionStatus status)
    {
        var isInTrial = IsInTrial(state);
        return status switch
        {
            BillingSubscriptionStatus.Unsubscribed => true,
            BillingSubscriptionStatus.Canceled => true,
            BillingSubscriptionStatus.Activated when isInTrial => true,
            _ => false
        };
    }

    private static bool IsInTrial(this SubscriptionMetadata state)
    {
        if (state.TryGetValue(ChargebeeStateInterpreter.Constants.MetadataProperties.SubscriptionStatus,
                out var status))
        {
            return status.HasValue()
                   && status == Subscription.StatusEnum.InTrial.ToString();
        }

        return false;
    }
}