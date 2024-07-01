using ChargeBee.Models;
using Common;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using UnitTesting.Common;
using Xunit;
using Constants = Infrastructure.Shared.ApplicationServices.External.ChargebeeStateInterpreter.Constants;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class ChargebeeStateInterpreterSpec
{
    private readonly ChargebeeStateInterpreter _interpreter;

    public ChargebeeStateInterpreterSpec()
    {
        _interpreter = new ChargebeeStateInterpreter("astandardplanid, anotherstandardplanid");
    }

    [Fact]
    public void WhenGetProviderName_ThenReturnsName()
    {
        var result = _interpreter.ProviderName;

        result.Should().Be(Constants.ProviderName);
    }

    [Fact]
    public void WhenSetInitialProviderStateAndDifferentProviderName_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.Validation, Resources.BillingProvider_ProviderNameNotMatch);
    }

    [Fact]
    public void WhenSetInitialProviderStateAndSubscriptionIdNotPresent_ThenReturnsError()
    {
        var provider = BillingProvider.Create(Constants.ProviderName,
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            Constants.MetadataProperties.SubscriptionId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderStateAndCustomerIdNotPresent_ThenReturnsError()
    {
        var provider = BillingProvider.Create(Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriberid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            Constants.MetadataProperties.CustomerId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderState_ThenReturnsProviderState()
    {
        var provider = BillingProvider.Create(Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriberid" },
                    { Constants.MetadataProperties.CustomerId, "abuyerid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Constants.ProviderName);
        result.Value.State.Count.Should().Be(2);
        result.Value.State[Constants.MetadataProperties.SubscriptionId].Should()
            .Be("asubscriberid");
        result.Value.State[Constants.MetadataProperties.CustomerId].Should().Be("abuyerid");
    }

    [Fact]
    public void WhenGetBuyerReferenceAndNotExists_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.GetBuyerReference(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            Constants.MetadataProperties.CustomerId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenGetBuyerReference_ThenReturnsCustomerId()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.CustomerId, "acustomerid" }
                })
            .Value;

        var result = _interpreter.GetBuyerReference(provider);

        result.Should().BeSuccess();
        result.Value.Should().Be("acustomerid");
    }

    [Fact]
    public void WhenGetSubscriptionReferenceAndNotExists_ThenReturnsNone()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.GetSubscriptionReference(provider);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
    }

    [Fact]
    public void WhenGetSubscriptionReference_ThenReturnsSubscriptionId()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionReference(provider);

        result.Should().BeSuccess();
        result.Value.Should().Be("asubscriptionid");
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndUnsubscribed_ThenReturnsUnsubscribed()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.CustomerId, "acustomerid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeNone();
        result.Value.Status.Should().Be(ProviderStatus.Empty);
        result.Value.Plan.Should().Be(ProviderPlan.Empty);
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void
        WhenGetSubscriptionDetailsAndUnsubscribedButStillHasPaymentMethod_ThenReturnsSubscriptionWithPaymentMethod()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        Constants.MetadataProperties.PaymentMethodType,
                        Customer.CustomerPaymentMethod.TypeEnum.Card.ToString()
                    },
                    {
                        Constants.MetadataProperties.PaymentMethodStatus,
                        Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString()
                    }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeNone();
        result.Value.Status.Should().Be(ProviderStatus.Empty);
        result.Value.Plan.Should().Be(ProviderPlan.Empty);
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Type.Should().Be(BillingPaymentMethodType.Card);
        result.Value.PaymentMethod.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.PaymentMethod.ExpiresOn.Should().BeNone();
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndDeleted_ThenReturnsUnsubscribed()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    { Constants.MetadataProperties.SubscriptionDeleted, "true" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Unsubscribed);
        result.Value.Status.CanBeUnsubscribed.Should().BeTrue();
        result.Value.Status.CanBeCanceled.Should().BeFalse();
        result.Value.Status.CanceledDateUtc.Should().BeNone();
        result.Value.Plan.Should().Be(ProviderPlan.Empty);
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndInFuturePlan_ThenReturnsActivatedStatus()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.TrialEnd, "1" },
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    { Constants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Future.ToString() },
                    { Constants.MetadataProperties.SubscriptionDeleted, "false" },
                    { Constants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Activated);
        result.Value.Status.CanBeUnsubscribed.Should().BeFalse();
        result.Value.Status.CanBeCanceled.Should().BeTrue();
        result.Value.Status.CanceledDateUtc.Should().BeNone();
        result.Value.Plan.PlanId.Should().Be("astandardplanid");
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Standard);
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndInTrial_ThenReturnsTrialStatus()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.TrialEnd, "1" },
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    { Constants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.InTrial.ToString() },
                    { Constants.MetadataProperties.SubscriptionDeleted, "false" },
                    { Constants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Activated);
        result.Value.Status.CanBeUnsubscribed.Should().BeTrue();
        result.Value.Status.CanBeCanceled.Should().BeTrue();
        result.Value.Status.CanceledDateUtc.Should().BeNone();
        result.Value.Plan.PlanId.Should().Be("astandardplanid");
        result.Value.Plan.IsTrial.Should().BeTrue();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Standard);
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndCanceledFuturePlan_ThenReturnsCanceledStatus()
    {
        var canceledAt = DateTime.UtcNow.ToNearestSecond().AddMonths(1);
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.TrialEnd, "1" },
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    { Constants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.NonRenewing.ToString() },
                    { Constants.MetadataProperties.CanceledAt, canceledAt.ToIso8601() },
                    { Constants.MetadataProperties.SubscriptionDeleted, "false" },
                    { Constants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Canceling);
        result.Value.Status.CanBeUnsubscribed.Should().BeFalse();
        result.Value.Status.CanBeCanceled.Should().BeFalse();
        result.Value.Status.CanceledDateUtc.Should().BeSome(canceledAt);
        result.Value.Plan.PlanId.Should().Be("astandardplanid");
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Standard);
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsAndCanceledTrial_ThenReturnsCanceledStatus()
    {
        var canceledAt = DateTime.UtcNow.ToNearestSecond();
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.TrialEnd, "1" },
                    { Constants.MetadataProperties.CustomerId, "acustomerid" },
                    { Constants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Cancelled.ToString() },
                    { Constants.MetadataProperties.CanceledAt, canceledAt.ToIso8601() },
                    { Constants.MetadataProperties.SubscriptionDeleted, "false" },
                    { Constants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Canceled);
        result.Value.Status.CanBeUnsubscribed.Should().BeTrue();
        result.Value.Status.CanBeCanceled.Should().BeFalse();
        result.Value.Status.CanceledDateUtc.Should().BeSome(canceledAt);
        result.Value.Plan.PlanId.Should().Be("astandardplanid");
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Unsubscribed);
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Period.Should().Be(ProviderPlanPeriod.Empty);
        result.Value.PaymentMethod.Should().Be(ProviderPaymentMethod.Empty);
        result.Value.Invoice.Should().Be(ProviderInvoice.Default);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsWithPlanDetails_ThenReturnsSubscriptionWithPlan()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.TrialEnd, "1" },
                    { Constants.MetadataProperties.SubscriptionStatus, Subscription.StatusEnum.Active.ToString() },
                    { Constants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.Plan.PlanId.Should().Be("astandardplanid");
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Standard);
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
    }

    [Fact]
    public void WhenGetSubscriptionDetailsWithPeriodDetails_ThenReturnsSubscriptionWithPeriod()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.BillingPeriodValue, "9" },
                    { Constants.MetadataProperties.BillingPeriodUnit, "day" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Period.Frequency.Should().Be(9);
        result.Value.Period.Unit.Should().Be(BillingFrequencyUnit.Day);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsWithInvoiceDetails_ThenReturnsSubscriptionWithInvoice()
    {
        var nextBilling = DateTime.UtcNow.ToNearestSecond();
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { Constants.MetadataProperties.NextBillingAt, nextBilling.ToIso8601() },
                    { Constants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code },
                    { Constants.MetadataProperties.BillingAmount, "3" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.Invoice.Amount.Should().Be(0.03M);
        result.Value.Invoice.CurrencyCode.Currency.Should().Be(CurrencyCodes.Default);
        result.Value.Invoice.NextUtc.Should().BeSome(nextBilling);
    }

    [Fact]
    public void WhenGetSubscriptionDetailsWithPaymentMethodDetails_ThenReturnsSubscriptionWithPaymentMethod()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { Constants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    {
                        Constants.MetadataProperties.PaymentMethodType,
                        Customer.CustomerPaymentMethod.TypeEnum.Card.ToString()
                    },
                    {
                        Constants.MetadataProperties.PaymentMethodStatus,
                        Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString()
                    }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid");
        result.Value.PaymentMethod.Type.Should().Be(BillingPaymentMethodType.Card);
        result.Value.PaymentMethod.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.PaymentMethod.ExpiresOn.Should().BeNone();
    }
}