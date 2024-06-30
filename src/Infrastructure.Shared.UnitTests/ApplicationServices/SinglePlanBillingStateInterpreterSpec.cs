using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class SinglePlanBillingStateInterpreterSpec
{
    private readonly SinglePlanBillingStateInterpreter _interpreter;

    public SinglePlanBillingStateInterpreterSpec()
    {
        _interpreter = new SinglePlanBillingStateInterpreter();
    }

    [Fact]
    public void WhenGetProviderName_ThenReturnsName()
    {
        var result = _interpreter.ProviderName;

        result.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
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
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId,
            typeof(SinglePlanBillingStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderStateAndBuyerIdNotPresent_ThenReturnsError()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriberid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId,
            typeof(SinglePlanBillingStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenSetInitialProviderState_ThenReturnsProviderState()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
                new SubscriptionMetadata
                {
                    { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriberid" },
                    { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" }
                })
            .Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
        result.Value.State.Count.Should().Be(2);
        result.Value.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId].Should()
            .Be("asubscriberid");
        result.Value.State[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId].Should()
            .Be("abuyerid");
    }

    [Fact]
    public void WhenGetSubscriptionReferenceAndSubscriptionIdNotExist_ThenReturnsNone()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.GetSubscriptionReference(provider);

        result.Value.Should().BeNone();
    }

    [Fact]
    public void WhenGetSubscriptionReferenceAndExists_ThenReturnsSubscriptionId()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
            }).Value;

        var result = _interpreter.GetSubscriptionReference(provider);

        result.Value.Should().BeSome("asubscriptionid");
    }

    [Fact]
    public void WhenGetBuyerReferenceAndBuyerIdNotExist_ThenReturnsError()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
                new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _interpreter.GetBuyerReference(provider);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.BillingProvider_PropertyNotFound.Format(
                SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId,
                typeof(SinglePlanBillingStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenGetBuyerReference_ThenReturnsBuyerId()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" }
            }).Value;

        var result = _interpreter.GetBuyerReference(provider);

        result.Should().Be("abuyerid");
    }

    [Fact]
    public void WhenGetBillingSubscriptionAndUnsubscribed_ThenAlwaysReturnsEmptySubscription()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" }
            }).Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Value.SubscriptionReference.Should().BeNone();
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Unsubscribed);
        result.Value.Status.CanceledDateUtc.Should().BeNone();
        result.Value.Status.CanBeUnsubscribed.Should().BeTrue();
        result.Value.Plan.PlanId.Should().BeNone();
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Unsubscribed);
        result.Value.Period.Frequency.Should().Be(0);
        result.Value.Period.Unit.Should().Be(BillingFrequencyUnit.Eternity);
        result.Value.Invoice.CurrencyCode.Currency.Should().Be(CurrencyCodes.Default);
        result.Value.Invoice.NextUtc.Should().BeNone();
        result.Value.Invoice.Amount.Should().Be(0);
        result.Value.PaymentMethod.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.PaymentMethod.Type.Should().Be(BillingPaymentMethodType.Other);
        result.Value.PaymentMethod.ExpiresOn.Should().BeNone();
    }

    [Fact]
    public void WhenGetBillingSubscription_ThenAlwaysReturnsStandardPlan()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
            }).Value;

        var result = _interpreter.GetSubscriptionDetails(provider);

        result.Value.SubscriptionReference.Should().Be("asubscriptionid".ToId());
        result.Value.Status.Status.Should().Be(BillingSubscriptionStatus.Activated);
        result.Value.Status.CanceledDateUtc.Should().BeNone();
        result.Value.Status.CanBeUnsubscribed.Should().BeTrue();
        result.Value.Plan.PlanId.Should().Be(SinglePlanBillingStateInterpreter.Constants.DefaultPlanId);
        result.Value.Plan.IsTrial.Should().BeFalse();
        result.Value.Plan.TrialEndDateUtc.Should().BeNone();
        result.Value.Plan.Tier.Should().Be(BillingSubscriptionTier.Standard);
        result.Value.Period.Frequency.Should().Be(0);
        result.Value.Period.Unit.Should().Be(BillingFrequencyUnit.Eternity);
        result.Value.Invoice.CurrencyCode.Currency.Should().Be(CurrencyCodes.Default);
        result.Value.Invoice.NextUtc.Should().BeNone();
        result.Value.Invoice.Amount.Should().Be(0);
        result.Value.PaymentMethod.Status.Should().Be(BillingPaymentMethodStatus.Valid);
        result.Value.PaymentMethod.Type.Should().Be(BillingPaymentMethodType.Other);
        result.Value.PaymentMethod.ExpiresOn.Should().BeNone();
    }

    [Fact]
    public void WhenTranslateSubscribedProviderAndNotForThisProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("anotherprovider", new SubscriptionMetadata
        {
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" },
            { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
        }).Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.Validation, Resources.BillingProvider_ProviderNameNotMatch);
    }

    [Fact]
    public void WhenTranslateSubscribedProviderAndMissingBuyerId_ThenReturnsError()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
            }).Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId,
            typeof(SinglePlanBillingStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenTranslateSubscribedProviderAndMissingSubscriptionId_ThenReturnsError()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" }
            }).Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BillingProvider_PropertyNotFound.Format(
            SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId,
            typeof(SinglePlanBillingStateInterpreter).FullName!));
    }

    [Fact]
    public void WhenTranslateSubscribedProvider_ThenReturnsSameProvider()
    {
        var provider = BillingProvider.Create(SinglePlanBillingStateInterpreter.Constants.ProviderName,
            new SubscriptionMetadata
            {
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId, "abuyerid" },
                { SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId, "asubscriptionid" }
            }).Value;

        var result = _interpreter.SetInitialProviderState(provider);

        result.Should().BeSuccess();
        result.Value.Should().Be(provider);
    }
}