using Application.Resources.Shared;
using ChargeBee.Models;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using UnitTesting.Common;
using Xunit;
using Constants = Infrastructure.External.ApplicationServices.ChargebeeStateInterpreter.Constants;
using Subscription = ChargeBee.Models.Subscription;

namespace Infrastructure.External.UnitTests.ApplicationServices;

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
            ChargebeeConstants.MetadataProperties.SubscriptionId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderStateAndCustomerIdNotPresent_ThenReturnsError()
    {
        var provider = BillingProvider.Create(Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriberid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            ChargebeeConstants.MetadataProperties.CustomerId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderState_ThenReturnsProviderState()
    {
        var provider = BillingProvider.Create(Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriberid" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "abuyerid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Constants.ProviderName);
        result.Value.State.Count.Should().Be(2);
        result.Value.State[ChargebeeConstants.MetadataProperties.SubscriptionId].Should()
            .Be("asubscriberid");
        result.Value.State[ChargebeeConstants.MetadataProperties.CustomerId].Should().Be("abuyerid");
    }

    [Fact]
    public void WhenGetBuyerReferenceAndNotExists_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.GetBuyerReference(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            ChargebeeConstants.MetadataProperties.CustomerId,
            typeof(ChargebeeStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenGetBuyerReference_ThenReturnsCustomerId()
    {
        var provider = BillingProvider.Create("aprovidername",
                new SubscriptionMetadata
                {
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" }
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" }
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
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" }
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
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        ChargebeeConstants.MetadataProperties.PaymentMethodType,
                        Customer.CustomerPaymentMethod.TypeEnum.Card.ToString()
                    },
                    {
                        ChargebeeConstants.MetadataProperties.PaymentMethodStatus,
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "true" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.TrialEnd, "1" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                        Subscription.StatusEnum.Future.ToString()
                    },
                    { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "false" },
                    { ChargebeeConstants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.TrialEnd, "1" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                        Subscription.StatusEnum.InTrial.ToString()
                    },
                    { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "false" },
                    { ChargebeeConstants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.TrialEnd, "1" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                        Subscription.StatusEnum.NonRenewing.ToString()
                    },
                    { ChargebeeConstants.MetadataProperties.CanceledAt, canceledAt.ToIso8601() },
                    { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "false" },
                    { ChargebeeConstants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.TrialEnd, "1" },
                    { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                    {
                        ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                        Subscription.StatusEnum.Cancelled.ToString()
                    },
                    { ChargebeeConstants.MetadataProperties.CanceledAt, canceledAt.ToIso8601() },
                    { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "false" },
                    { ChargebeeConstants.MetadataProperties.PlanId, "astandardplanid" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.TrialEnd, "1" },
                    {
                        ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                        Subscription.StatusEnum.Active.ToString()
                    },
                    { ChargebeeConstants.MetadataProperties.PlanId, "astandardplanid" }
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.BillingPeriodValue, "9" },
                    { ChargebeeConstants.MetadataProperties.BillingPeriodUnit, "day" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    { ChargebeeConstants.MetadataProperties.NextBillingAt, nextBilling.ToIso8601() },
                    { ChargebeeConstants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code },
                    { ChargebeeConstants.MetadataProperties.BillingAmount, "3" }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
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
                    { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                    {
                        ChargebeeConstants.MetadataProperties.PaymentMethodType,
                        Customer.CustomerPaymentMethod.TypeEnum.Card.ToString()
                    },
                    {
                        ChargebeeConstants.MetadataProperties.PaymentMethodStatus,
                        Customer.CustomerPaymentMethod.StatusEnum.Valid.ToString()
                    }
                })
            .Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Should().BeSuccess();
        result.Value.SubscriptionReference.Should().BeSome("asubscriptionid".ToId());
        result.Value.PaymentMethod.Type.Should().Be(BillingPaymentMethodType.Card);
        result.Value.PaymentMethod.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.PaymentMethod.ExpiresOn.Should().BeNone();
    }
}