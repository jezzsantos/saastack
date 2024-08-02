using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a simple working <see cref="IBillingProvider" /> until a 3rd party one is installed to replace this one.
///     Note: all subscribers are on the same plan and tier (<see cref="BillingSubscriptionTier.Standard" />),
///     and the plan cannot (effectively) be changed, but it can be canceled/unsubscribed.
/// </summary>
public sealed class SimpleBillingProvider : IBillingProvider
{
    public SimpleBillingProvider()
    {
        GatewayService = new InProcessInMemSimpleBillingGatewayService();
        StateInterpreter = new SinglePlanBillingStateInterpreter();
    }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }
}

/// <summary>
///     Provides a simple interpreter that maintains a single bare minimum plan that cannot be canceled or changed
/// </summary>
public sealed class SinglePlanBillingStateInterpreter : IBillingStateInterpreter
{
    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        if (current.State.TryGetValue(Constants.MetadataProperties.BuyerId, out var reference))
        {
            return reference;
        }

        return Error.RuleViolation(
            Resources.BillingProvider_PropertyNotFound.Format(Constants.MetadataProperties.BuyerId,
                GetType().FullName!));
    }

    /// <summary>
    ///     Either the subscription is unsubscribed, or the plan is fixed, and does not vary.
    /// </summary>
    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        if (!current.State.TryGetValue(Constants.MetadataProperties.SubscriptionId, out var subscriptionId))
        {
            var billingStatus =
                ProviderStatus.Create(BillingSubscriptionStatus.Unsubscribed, Optional<DateTime>.None, true);
            if (billingStatus.IsFailure)
            {
                return billingStatus.Error;
            }

            // Always a valid payment method
            var paymentMethod =
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Other, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None);
            if (paymentMethod.IsFailure)
            {
                return paymentMethod.Error;
            }

            // No plan
            return ProviderSubscription.Create(billingStatus.Value, paymentMethod.Value);
        }
        else
        {
            var billingStatus =
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true);
            if (billingStatus.IsFailure)
            {
                return billingStatus.Error;
            }

            // Fixed plan
            var plan = ProviderPlan.Create(Constants.DefaultPlanId, false, Optional<DateTime>.None,
                BillingSubscriptionTier.Standard);
            if (plan.IsFailure)
            {
                return plan.Error;
            }

            // Always a valid payment method
            var paymentMethod =
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Other, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None);
            if (paymentMethod.IsFailure)
            {
                return paymentMethod.Error;
            }

            return ProviderSubscription.Create(subscriptionId.ToId(), billingStatus.Value, plan.Value,
                ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, paymentMethod.Value);
        }
    }

    public Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current)
    {
        if (!current.State.TryGetValue(Constants.MetadataProperties.SubscriptionId, out var reference))
        {
            return Optional<string>.None;
        }

        return reference.ToOptional();
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

        if (!provider.State.TryGetValue(Constants.MetadataProperties.BuyerId, out _))
        {
            return Error.RuleViolation(
                Resources.BillingProvider_PropertyNotFound.Format(Constants.MetadataProperties.BuyerId,
                    GetType().FullName!));
        }

        return provider;
    }

    public static class Constants
    {
        public const string DefaultPlanId = "_simple_standard";
        public const string ProviderName = "simple_billing_provider";

        public static class MetadataProperties
        {
            public const string BuyerId = "BuyerId";
            public const string SubscriptionId = "SubscriptionId";
        }
    }
}

/// <summary>
///     Provides a simple in-memory gateway that has very basic behaviour (no remote service)
/// </summary>
public class InProcessInMemSimpleBillingGatewayService : IBillingGatewayService
{
    private const string SubscriptionIdPrefix = "simplesub";

    /// <summary>
    ///     Removes the subscription from the state when canceled
    /// </summary>
    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            {
                SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId,
                provider.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId]
            }
        });
    }

    /// <summary>
    ///     Resets the state, since the current plan cannot be (effectively) changed
    /// </summary>
    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            {
                SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId,
                provider.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId]
            },
            {
                SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId,
                provider.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId]
            }
        });
    }

    /// <summary>
    ///     There is only one fixed plan, at zero-cost
    /// </summary>
    public Task<Result<PricingPlans, Error>> ListAllPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<PricingPlans, Error>>(new PricingPlans
        {
            Eternally =
            [
                new PricingPlan
                {
                    Id = SinglePlanBillingStateInterpreter.Constants.DefaultPlanId,
                    IsRecommended = true,
                    DisplayName = Resources.InProcessInMemBillingGatewayService_BasicPlan_DisplayName,
                    Description = Resources.InProcessInMemBillingGatewayService_BasicPlan_Description,
                    Notes = Resources.InProcessInMemBillingGatewayService_BasicPlan_Notes,
                    SetupCost = 0M,
                    Cost = 0M,
                    Currency = CurrencyCodes.Default.Code,
                    Period = new PlanPeriod
                    {
                        Frequency = 1,
                        Unit = PeriodFrequencyUnit.Eternity
                    },
                    Trial = new SubscriptionTrialPeriod
                    {
                        HasTrial = false,
                        Frequency = 0,
                        Unit = PeriodFrequencyUnit.Eternity
                    },
                    FeatureSection =
                    [
                        new PricingFeatureSection
                        {
                            Description = "",
                            Features =
                            [
                                new PricingFeatureItem
                                {
                                    Description = Resources
                                        .InProcessInMemBillingGatewayService_BasicPlan_Feature1_Description,
                                    IsIncluded = true
                                }
                            ]
                        }
                    ]
                }
            ],
            Annually = [],
            Monthly = [],
            Weekly = [],
            Daily = []
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> RestoreBuyerAsync(ICallerContext caller, SubscriptionBuyer buyer,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, buyer.Id },
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, GenerateSubscriptionId() }
        });
    }

    /// <summary>
    ///     Returns a zero-invoice for every 1st of the month in the date range
    /// </summary>
    public Task<Result<List<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller, BillingProvider provider,
        DateTime fromUtc, DateTime toUtc,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var dates = new List<DateTime>();
        var firstMonth = new DateTime(fromUtc.Year, fromUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (fromUtc > firstMonth)
        {
            firstMonth = firstMonth.AddMonths(1);
        }

        dates.Add(firstMonth);
        if (toUtc > firstMonth)
        {
            var lastMonth = firstMonth;
            while (lastMonth < toUtc)
            {
                lastMonth = lastMonth.AddMonths(1);
                dates.Add(lastMonth);
            }
        }

        var invoices = dates
            .Select(CreateInvoice)
            .ToList();

        return Task.FromResult<Result<List<Invoice>, Error>>(invoices);

        Invoice CreateInvoice(DateTime date)
        {
            var id = $"invoice_{date:FFFFFF}";
            var reference =
                provider.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId];
            var currency = CurrencyCodes.Default.Code;
            return new Invoice
            {
                Id = id,
                Amount = 0M,
                Currency = currency,
                Status = InvoiceStatus.Paid,
                TaxAmount = 0M,
                IncludesTax = true,
                InvoicedOnUtc = date,
                LineItems =
                [
                    new InvoiceLineItem
                    {
                        Amount = 0,
                        Currency = currency,
                        Description = "Free Service",
                        IsTaxed = true,
                        Reference = reference,
                        TaxAmount = 0
                    }
                ],
                Notes =
                [
                    new InvoiceNote
                    {
                        Description = "Some notes"
                    }
                ],
                Payment = new InvoiceItemPayment
                {
                    Amount = 0M,
                    Currency = currency,
                    PaidOnUtc = date,
                    Reference = "Paid"
                },
                PeriodEndUtc = date,
                PeriodStartUtc = date.AddMonths(-1)
            };
        }
    }

    /// <summary>
    ///     Subscribes the buyer to the default plan, with a new subscription
    /// </summary>
    public Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller,
        SubscriptionBuyer buyer, SubscribeOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, buyer.Id },
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, GenerateSubscriptionId() }
        });
    }

    /// <summary>
    ///     Transfers the existing subscription plan to a new buyer
    /// </summary>
    public Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, options.TransfereeBuyer.Id },
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, GenerateSubscriptionId() }
        });
    }

    private static string GenerateSubscriptionId()
    {
        return $"{SubscriptionIdPrefix}_{Guid.NewGuid():N}";
    }
}