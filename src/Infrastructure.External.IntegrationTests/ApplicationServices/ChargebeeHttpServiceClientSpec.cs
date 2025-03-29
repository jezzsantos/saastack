using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using ChargeBee.Models;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;
using Constants = Infrastructure.External.ApplicationServices.ChargebeeStateInterpreter.Constants;
using Subscription = ChargeBee.Models.Subscription;

namespace Infrastructure.External.IntegrationTests.ApplicationServices;

public abstract class ChargebeeHttpServiceClientSetupSpec : ExternalApiSpec
{
    private const string TestCustomerIdPrefix = "testorganizationid";
    private const string TestUserIdPrefix = "testuserid";
    protected static readonly TestPlan SetupPlan = new("SetupFee", 10M, "A setup fee", false, false, false);
    protected static readonly TestFeature TestFeature1 = new("Feature1", "A feature1");
    protected static readonly TestPlan[] TestPlans =
    [
        new("Trial", 50M, "Trial plan", true, false, false),
        new("Paid", 100M, "Paid plan", false, true, false),
        new("PaidWithSetup", 100M, "PaidWithSetup plan", false, true, true)
    ];

    protected ChargebeeHttpServiceClientSetupSpec(ExternalApiSetup setup) : base(setup, null,
        _ =>
        {
            var settings = setup.GetRequiredService<IConfigurationSettings>();
            var serviceClient = new ChargebeeHttpServiceClient(NoOpRecorder.Instance, settings);
            var productFamilyId = settings.Platform.GetString(Constants.ProductFamilyIdSettingName);
            SetupTestingSandboxAsync(new TestCaller(), serviceClient, productFamilyId).GetAwaiter().GetResult();
        })
    {
        Caller = new TestCaller();
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        ServiceClient = new ChargebeeHttpServiceClient(NoOpRecorder.Instance, settings);
        ProductFamilyId = settings.Platform.GetString(Constants.ProductFamilyIdSettingName);
    }

    protected ICallerContext Caller { get; }

    protected string ProductFamilyId { get; }

    protected ChargebeeHttpServiceClient ServiceClient { get; }

    protected async Task<BillingProvider> CancelSubscriptionAsync(BillingProvider provider,
        CancelSubscriptionOptions options)
    {
        var result = await ServiceClient.CancelSubscriptionAsync(Caller, options, provider, CancellationToken.None);
        return BillingProvider.Create(Constants.ProviderName, result.Value)
            .Value;
    }

    protected static SubscriptionBuyer CreateBuyer()
    {
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        return new SubscriptionBuyer
        {
            Address = new ProfileAddress
            {
                CountryCode = CountryCodes.Default.ToString()
            },
            Subscriber = new Subscriber
            {
                EntityId = $"{TestCustomerIdPrefix}{random}",
                EntityType = nameof(Organization)
            },
            EmailAddress = "auser@company.com",
            Id = TestUserIdPrefix,
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            PhoneNumber = null
        };
    }

    protected async Task<(SubscriptionBuyer Buyer, Customer Customer, BillingProvider Provider)> CreateCustomerAsync()
    {
#if TESTINGONLY
        var buyer = CreateBuyer();
        var customer = (await ServiceClient.CreateCustomerAsync(Caller, buyer, CancellationToken.None)).Value;
        var provider = BillingProvider
            .Create(Constants.ProviderName, customer.ToCustomerState())
            .Value;

        return (buyer, customer, provider);
#else
        await Task.CompletedTask;
        return (null!, null!, null!);
#endif
    }

    /// <summary>
    ///     Returns a new customer with a valid payment source and subscribes them to the given plan, or initial plan
    /// </summary>
    protected async Task<BillingProvider> SubscribeCustomerWithCardAsync(string? planId = null)
    {
        var (buyer, customer, _) = await CreateCustomerAsync();
#if TESTINGONLY
        (await ServiceClient.CreateCustomerPaymentMethod(Caller, customer.Id, CancellationToken.None))
            .ThrowOnError();
#endif

        var options = SubscribeOptions.Immediately;
#if TESTINGONLY
        if (planId.HasValue())
        {
            options.PlanId = planId;
        }
#endif

        var subscribed = await ServiceClient.SubscribeAsync(Caller, buyer,
            options, CancellationToken.None);
        return BillingProvider.Create(Constants.ProviderName, subscribed.Value)
            .Value;
    }

    protected static string ToBillingAmount(TestPlan plan, CurrencyCodeIso4217? currency = null)
    {
        return CurrencyCodes.ToMinorUnit(currency ?? CurrencyCodes.Default, plan.Price).ToString();
    }

    protected async Task<BillingProvider> UnsubscribeAsync(BillingProvider provider)
    {
        var customerId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CustomerId)!;
        var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId)!;
#if TESTINGONLY
        var result = await ServiceClient.DeleteSubscriptionAsync(Caller, subscriptionId, CancellationToken.None);
        if (result.IsFailure)
        {
            return provider;
        }
#endif

        return BillingProvider.Create(Constants.ProviderName, new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, customerId },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, subscriptionId },
            { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "true" }
        }).Value;
    }

    private static async Task SetupTestingSandboxAsync(ICallerContext caller, ChargebeeHttpServiceClient serviceClient,
        string productFamilyId)
    {
#if TESTINGONLY
        // Cleanup any existing data
        var subscriptions =
            (await serviceClient.SearchAllSubscriptionsAsync(caller, new SearchOptions(),
                CancellationToken.None))
            .Value;
        foreach (var subscription in subscriptions.Where(sub => sub.CustomerId.StartsWith(TestCustomerIdPrefix)))
        {
            (await serviceClient.DeleteSubscriptionAsync(caller, subscription.Id, CancellationToken.None))
                .ThrowOnError();
            (await serviceClient.DeleteCustomerAsync(caller, subscription.CustomerId, CancellationToken.None))
                .ThrowOnError();
        }

        // Cleanup any orphaned customers
        var customers =
            (await serviceClient.SearchAllCustomersAsync(caller, new SearchOptions(), CancellationToken.None))
            .Value;
        foreach (var customer in customers.Where(c => c.Id.StartsWith(TestCustomerIdPrefix)
                                                      && c.Deleted == false))
        {
            // Ignore errors (e.g. customer has already been scheduled for delete)
            await serviceClient.DeleteCustomerAsync(caller, customer.Id, CancellationToken.None);
        }

        var plans = (await serviceClient.SearchAllPlansAsync(caller, new SearchOptions(), CancellationToken.None))
            .Value;
        foreach (var plan in plans)
        {
            var features =
                (await serviceClient.SearchAllPlanFeaturesAsync(caller, plan.Id, new SearchOptions(),
                    CancellationToken.None)).Value;
            foreach (var feature in features)
            {
                (await serviceClient.RemovePlanFeatureAsync(caller, plan.Id, feature.FeatureId,
                    CancellationToken.None)).ThrowOnError();
                (await serviceClient.DeleteFeatureAsync(caller, feature.FeatureId, CancellationToken.None))
                    .ThrowOnError();
            }

            (await serviceClient.DeletePlanAndPricesAsync(caller, plan.Id, CancellationToken.None)).ThrowOnError();
        }

        var charges =
            (await serviceClient.SearchAllChargesAsync(caller, new SearchOptions(), CancellationToken.None)).Value;
        foreach (var charge in charges)
        {
            (await serviceClient.DeleteChargeAndPricesAsync(caller, charge.Id, CancellationToken.None))
                .ThrowOnError();
        }

        // Create new test data (reactivate archived items if necessary)
        await serviceClient.CreateProductFamilySafelyAsync(caller, productFamilyId, CancellationToken.None);
        var feature1 = (await serviceClient.CreateFeatureSafelyAsync(caller, TestFeature1.Name,
            TestFeature1.Description, CancellationToken.None)).Value;
        var setupCharge = (await serviceClient.CreateChargeSafelyAsync(caller, productFamilyId, SetupPlan.Name,
            SetupPlan.Description, CancellationToken.None)).Value;
        var setupChargePrice = (await serviceClient.CreateOneOffItemPriceAsync(caller, setupCharge.Id,
            SetupPlan.Description, CurrencyCodes.Default, SetupPlan.Price, CancellationToken.None)).Value;
        foreach (var testPlan in TestPlans)
        {
            var plan = (await serviceClient.CreatePlanSafelyAsync(caller, productFamilyId, testPlan.Name,
                testPlan.Description, CancellationToken.None)).Value;

            (await serviceClient.CreateMonthlyRecurringItemPriceAsync(caller, plan.Id, testPlan.Description,
                CurrencyCodes.Default, testPlan.Price, testPlan.HasTrial, CancellationToken.None)).ThrowOnError();

            if (testPlan.HasFeature)
            {
                (await serviceClient.AddPlanFeatureAsync(caller, plan.Id, feature1.Id, CancellationToken.None))
                    .ThrowOnError();
            }

            if (testPlan.HasSetupCharge)
            {
                (await serviceClient.AddPlanChargeAsync(caller, plan.Id, setupChargePrice.ItemId,
                    CancellationToken.None)).ThrowOnError();
            }
        }
#endif
    }

    protected record TestPlan(
        string Name,
        decimal Price,
        string Description,
        bool HasTrial,
        bool HasFeature,
        bool HasSetupCharge)
    {
        public string PlanId => $"{Name}-USD-Monthly";
    }

    protected record TestFeature(
        string Name,
        string Description);
}

/// <summary>
///     These tests directly test the adapter against a live instance of Chargebee API.
///     Note: Some of the tests plans include a mandatory setup fee that requires a PaymentSource to be added by the
///     customer before they can subscribe to that plan. The setup fee is a one-time charge that is added to the first
///     invoice.
///     Note: you will have to set up Chargebee Timezone, and default Currency (in the Configure Page of the Portal)
/// </summary>
[UsedImplicitly]
public class ChargebeeHttpServiceClientSpec
{
    [Trait("Category", "Integration.External")]
    [Collection("External")]
    public class GivenNoSubscriptions : ChargebeeHttpServiceClientSetupSpec
    {
        public GivenNoSubscriptions(ExternalApiSetup setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenListAllPricingPlansAsync_ThenReturnsPlans()
        {
            var result = await ServiceClient.ListAllPricingPlansAsync(Caller, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Eternally.Should().BeEmpty();
            result.Value.Annually.Should().BeEmpty();
            result.Value.Weekly.Should().BeEmpty();
            result.Value.Daily.Should().BeEmpty();
            result.Value.Monthly.Count.Should().Be(3);
            var monthlyPlan1 = result.Value.Monthly[0];
            monthlyPlan1.Id.Should().Be(TestPlans[0].PlanId);
            monthlyPlan1.Cost.Should().Be(TestPlans[0].Price);
            monthlyPlan1.Currency.Should().Be(CurrencyCodes.Default.Code);
            monthlyPlan1.Description.Should().Be(TestPlans[0].Description);
            monthlyPlan1.DisplayName.Should().Be(TestPlans[0].Name);
            monthlyPlan1.FeatureSection.Count.Should().Be(0);
            monthlyPlan1.IsRecommended.Should().BeFalse();
            monthlyPlan1.Notes.Should().Be(TestPlans[0].Description);
            monthlyPlan1.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan1.Period.Frequency.Should().Be(1);
            monthlyPlan1.SetupCost.Should().Be(0);
            monthlyPlan1.Trial!.HasTrial.Should().BeTrue();
            monthlyPlan1.Trial.Frequency.Should().Be(7);
            monthlyPlan1.Trial.Unit.Should().Be(PeriodFrequencyUnit.Day);

            var monthlyPlan2 = result.Value.Monthly[1];
            monthlyPlan2.Id.Should().Be(TestPlans[2].PlanId);
            monthlyPlan2.Cost.Should().Be(TestPlans[2].Price);
            monthlyPlan2.Currency.Should().Be(CurrencyCodes.Default.Code);
            monthlyPlan2.Description.Should().Be(TestPlans[2].Description);
            monthlyPlan2.DisplayName.Should().Be(TestPlans[2].Name);
            monthlyPlan2.FeatureSection.Count.Should().Be(1);
            monthlyPlan2.FeatureSection[0].Description.Should().BeNull();
            monthlyPlan2.FeatureSection[0].Features.Count.Should().Be(1);
            monthlyPlan2.FeatureSection[0].Features[0].IsIncluded.Should().BeTrue();
            monthlyPlan2.FeatureSection[0].Features[0].Description.Should().Be(TestFeature1.Description);
            monthlyPlan2.IsRecommended.Should().BeFalse();
            monthlyPlan2.Notes.Should().Be(TestPlans[2].Description);
            monthlyPlan2.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan2.Period.Frequency.Should().Be(1);
            monthlyPlan2.SetupCost.Should().Be(SetupPlan.Price);
            monthlyPlan2.Trial.Should().BeNull();

            var monthlyPlan3 = result.Value.Monthly[2];
            monthlyPlan3.Id.Should().Be(TestPlans[1].PlanId);
            monthlyPlan3.Cost.Should().Be(TestPlans[1].Price);
            monthlyPlan3.Currency.Should().Be(CurrencyCodes.Default.Code);
            monthlyPlan3.Description.Should().Be(TestPlans[1].Description);
            monthlyPlan3.DisplayName.Should().Be(TestPlans[1].Name);
            monthlyPlan3.FeatureSection.Count.Should().Be(1);
            monthlyPlan3.FeatureSection[0].Description.Should().BeNull();
            monthlyPlan3.FeatureSection[0].Features.Count.Should().Be(1);
            monthlyPlan3.FeatureSection[0].Features[0].IsIncluded.Should().BeTrue();
            monthlyPlan3.FeatureSection[0].Features[0].Description.Should().Be(TestFeature1.Description);
            monthlyPlan3.IsRecommended.Should().BeFalse();
            monthlyPlan3.Notes.Should().Be(TestPlans[1].Description);
            monthlyPlan3.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan3.Period.Frequency.Should().Be(1);
            monthlyPlan3.SetupCost.Should().Be(0);
            monthlyPlan3.Trial.Should().BeNull();
        }

        [Fact]
        public async Task WhenSearchAllInvoicesAsyncForUnsubscribedCustomer_ThenReturnsNoInvoices()
        {
            var (_, _, provider) = await CreateCustomerAsync();
            var from = DateTime.UtcNow.SubtractDays(30).ToNearestMinute();
            var to = from.AddDays(30).ToNearestMinute();

            var result = await ServiceClient.SearchAllInvoicesAsync(Caller, provider, from, to,
                new SearchOptions(), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Results.Count.Should().Be(0);
        }

        [Fact]
        public async Task WhenSubscribeNewCustomerToTrialPlanImmediately_ThenSubscribesImmediately()
        {
            var buyer = CreateBuyer();
            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, SubscribeOptions.Immediately, CancellationToken.None);

            var endOfTrial = DateTime.UtcNow.ToNearestSecond().AddDays(7);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(11);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingAmount,
                ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.CanceledAt);
            result.Value.Should()
                .Contain(ChargebeeConstants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.InTrial.ToString());
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.TrialEnd)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
        }

        [Fact]
        public async Task WhenSubscribeExistingCustomerToTrialPlanImmediately_ThenSubscribesImmediately()
        {
            var (buyer, _, _) = await CreateCustomerAsync();

            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, SubscribeOptions.Immediately, CancellationToken.None);

            var endOfTrial = DateTime.UtcNow.ToNearestSecond().AddDays(7);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(11);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingAmount,
                ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.CanceledAt);
            result.Value.Should()
                .Contain(ChargebeeConstants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.InTrial.ToString());
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.TrialEnd)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
        }

        [Fact]
        public async Task WhenSubscribeToTrialPlanInFuture_ThenSubscribesToStartInFuture()
        {
            var start = DateTime.UtcNow.ToNearestSecond().AddDays(1);
            var buyer = CreateBuyer();
            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, SubscribeOptions.AtScheduledTime(start),
                    CancellationToken.None);

            var endOfTrial = start.AddDays(7);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(11);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingAmount,
                ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.CanceledAt);
            result.Value.Should()
                .Contain(ChargebeeConstants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.Future.ToString());
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.TrialEnd)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
        }

        [Fact]
        public async Task WhenSubscribeToPaidPlanWithoutPaymentSource_ThenReturnsError()
        {
            var options = SubscribeOptions.Immediately;
#if TESTINGONLY
            options.PlanId = TestPlans[1].PlanId;
#endif
            var buyer = CreateBuyer();
            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, options, CancellationToken.None);

            result.Should().BeError(ErrorCode.PreconditionViolation, null,
                err => err.Message.Contains("payment_method_not_present"));
        }

        [Fact]
        public async Task WhenSubscribeToPaidPlanWithPaymentSource_ThenSubscribes()
        {
            var options = SubscribeOptions.Immediately;
#if TESTINGONLY
            options.PlanId = TestPlans[1].PlanId;
#endif
            var (buyer, customer, _) = await CreateCustomerAsync();
#if TESTINGONLY
            (await ServiceClient.CreateCustomerPaymentMethod(Caller, customer.Id, CancellationToken.None))
                .ThrowOnError();
#endif

            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, options, CancellationToken.None);

            var nextBilling = DateTime.UtcNow.ToNearestSecond().AddMonths(1);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(12);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingAmount,
                ToBillingAmount(TestPlans[1]));
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.CanceledAt);
            result.Value.Should()
                .Contain(ChargebeeConstants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(nextBilling));
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PaymentMethodStatus,
                PaymentSource.StatusEnum.Valid.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PaymentMethodType,
                Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PlanId, "Paid-USD-Monthly");
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(ChargebeeConstants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.Active.ToString());
            result.Value.Should().NotContainKey(ChargebeeConstants.MetadataProperties.TrialEnd);
        }

        [Fact]
        public async Task WhenRestoreBuyerAndCustomerExists_ThenUpdates()
        {
            var (buyer, customer, _) = await CreateCustomerAsync();
#if TESTINGONLY
            (await ServiceClient.CreateCustomerPaymentMethod(Caller, customer.Id, CancellationToken.None))
                .ThrowOnError();
#endif

            var result =
                await ServiceClient.RestoreBuyerAsync(Caller, buyer, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(3);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PaymentMethodStatus,
                PaymentSource.StatusEnum.Valid.ToString());
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.PaymentMethodType,
                Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
        }

        [Fact]
        public async Task WhenRestoreBuyerAndCustomerDeleted_ThenReCreates()
        {
            var buyer = CreateBuyer();

            var result =
                await ServiceClient.RestoreBuyerAsync(Caller, buyer, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(1);
            result.Value.Should().Contain(ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("External")]
    public class GivenASubscription : ChargebeeHttpServiceClientSetupSpec
    {
        public GivenASubscription(ExternalApiSetup setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenSearchAllInvoicesAsyncForSubscribedCustomer_ThenReturnsOneInvoice()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
            var from = now.ToNearestMinute();
            var to = from.AddMonths(1).ToNearestMinute();
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);

            var result = await ServiceClient.SearchAllInvoicesAsync(Caller, provider, from, to,
                new SearchOptions(), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Results.Count.Should().Be(1);
            result.Value.Results[0].Id.Should().NotBeEmpty();
            result.Value.Results[0].Amount.Should().Be(100);
            result.Value.Results[0].Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value.Results[0].IncludesTax.Should().BeFalse();
            result.Value.Results[0].InvoicedOnUtc!.Value.Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value.Results[0].LineItems.Count.Should().Be(1);
            result.Value.Results[0].LineItems[0].Amount.Should().Be(100);
            result.Value.Results[0].LineItems[0].Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value.Results[0].LineItems[0].Description.Should().Be("Paid");
            result.Value.Results[0].LineItems[0].IsTaxed.Should().BeFalse();
            result.Value.Results[0].LineItems[0].Reference.Should().NotBeEmpty();
            result.Value.Results[0].LineItems[0].TaxAmount.Should().Be(0);
            result.Value.Results[0].Notes.Count.Should().Be(1);
            result.Value.Results[0].Notes[0].Description.Should().Be("Paid plan");
            result.Value.Results[0].Payment!.Amount.Should().Be(100);
            result.Value.Results[0].Payment!.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value.Results[0].Payment!.PaidOnUtc!.Value.Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value.Results[0].Payment!.Reference.Should().NotBeEmpty();
            result.Value.Results[0].PeriodEndUtc!.Value.Should().BeNear(to, TimeSpan.FromMinutes(1));
            result.Value.Results[0].PeriodStartUtc!.Value.Should().BeNear(from, TimeSpan.FromMinutes(1));
            result.Value.Results[0].Status.Should().Be(InvoiceStatus.Paid);
            result.Value.Results[0].TaxAmount.Should().Be(0);
        }

        [Fact]
        public async Task WhenChangeSubscriptionPlanAsync_ThenUpgradesPlan()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);

            var result = await ServiceClient.ChangeSubscriptionPlanAsync(Caller, new ChangePlanOptions
            {
                Subscriber = new Subscriber
                {
                    EntityId = "anentityid",
                    EntityType = "anentitytype"
                },
                PlanId = TestPlans[2].PlanId
            }, provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.PlanId).Should()
                .Be(TestPlans[2].PlanId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
        }

        [Fact]
        public async Task WhenChangeSubscriptionPlanAsyncAndCancelled_ThenReactivatesAndUpgradesPlan()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            provider = await CancelSubscriptionAsync(provider, CancelSubscriptionOptions.Immediately);

            var result = await ServiceClient.ChangeSubscriptionPlanAsync(Caller, new ChangePlanOptions
            {
                Subscriber = new Subscriber
                {
                    EntityId = "anentityid",
                    EntityType = "anentitytype"
                },
                PlanId = TestPlans[2].PlanId
            }, provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should().BeNull();
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.PlanId).Should()
                .Be(TestPlans[2].PlanId);
        }

        [Fact]
        public async Task WhenChangeSubscriptionPlanAsyncAndCancelling_ThenRemoveCancellationAndUpgradesPlan()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            provider = await CancelSubscriptionAsync(provider, CancelSubscriptionOptions.EndOfTerm);

            var result = await ServiceClient.ChangeSubscriptionPlanAsync(Caller, new ChangePlanOptions
            {
                Subscriber = new Subscriber
                {
                    EntityId = "anentityid",
                    EntityType = "anentitytype"
                },
                PlanId = TestPlans[2].PlanId
            }, provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should().BeNull();
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.PlanId).Should()
                .Be(TestPlans[2].PlanId);
        }

        [Fact]
        public async Task WhenChangeSubscriptionPlanAsyncAndUnsubscribed_ThenResubscribesAndUpgradesPlan()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            provider = await UnsubscribeAsync(provider);

            var result = await ServiceClient.ChangeSubscriptionPlanAsync(Caller, new ChangePlanOptions
            {
                Subscriber = new Subscriber
                {
                    EntityId = "anentityid",
                    EntityType = "anentitytype"
                },
                PlanId = TestPlans[2].PlanId
            }, provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .NotBe(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should().BeNull();
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.PlanId).Should()
                .Be(TestPlans[2].PlanId);
        }

        [Fact]
        public async Task WhenCancelSubscriptionAsyncAndImmediately_ThenCancelledImmediately()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            var now = DateTime.UtcNow.ToNearestSecond();

            var result = await ServiceClient.CancelSubscriptionAsync(Caller, CancelSubscriptionOptions.Immediately,
                provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Cancelled.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should()
                .Match(x => x.IsNear(now));
        }

        [Fact]
        public async Task WhenCancelSubscriptionAsyncAndEndOfTerm_ThenCancelsLater()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            var now = DateTime.UtcNow.ToNearestSecond();
            var endOfTerm = now.AddMonths(1);

            var result = await ServiceClient.CancelSubscriptionAsync(Caller, CancelSubscriptionOptions.EndOfTerm,
                provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.NonRenewing.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should()
                .Match(x => x.IsNear(endOfTerm));
        }

        [Fact]
        public async Task WhenCancelSubscriptionAsyncInFuture_ThenCancelsInFuture()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            var now = DateTime.UtcNow.ToNearestSecond();
            var future = now.AddDays(2);

            var result = await ServiceClient.CancelSubscriptionAsync(Caller, new CancelSubscriptionOptions
                {
                    CancelWhen = CancelSubscriptionSchedule.Scheduled,
                    FutureTime = future
                },
                provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(10);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.NonRenewing.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should()
                .Match(x => x.IsNear(future));
        }

        [Fact]
        public async Task WhenTransferSubscriptionAsyncAndUnsubscribed_ThenResubscribesAndTransfers()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var customerId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CustomerId)!;
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);
            provider = await UnsubscribeAsync(provider);

            var result = await ServiceClient.TransferSubscriptionAsync(Caller, new TransferSubscriptionOptions
                {
                    TransfereeBuyer = new SubscriptionBuyer
                    {
                        Address = new ProfileAddress
                        {
                            CountryCode = CountryCodes.Default.ToString()
                        },
                        Subscriber = new Subscriber
                        {
                            EntityId = customerId,
                            EntityType = "anentitytype"
                        },
                        EmailAddress = "anotheruser@company.com",
                        Id = "anothertestuserid",
                        Name = new PersonName
                        {
                            FirstName = "anewfirstname",
                            LastName = "anewlastname"
                        }
                    },
                    PlanId = TestPlans[2].PlanId
                },
                provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(12);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CustomerId).Should().Be(customerId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .NotBe(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should().BeNull();
        }

        [Fact]
        public async Task WhenTransferSubscriptionAsync_ThenTransfers()
        {
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var customerId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CustomerId)!;
            var subscriptionId = provider.State.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId);

            var result = await ServiceClient.TransferSubscriptionAsync(Caller, new TransferSubscriptionOptions
                {
                    TransfereeBuyer = new SubscriptionBuyer
                    {
                        Address = new ProfileAddress
                        {
                            CountryCode = CountryCodes.Default.ToString()
                        },
                        Subscriber = new Subscriber
                        {
                            EntityId = customerId,
                            EntityType = "anentitytype"
                        },
                        EmailAddress = "anotheruser@company.com",
                        Id = "anothertestuserid",
                        Name = new PersonName
                        {
                            FirstName = "anewfirstname",
                            LastName = "anewlastname"
                        }
                    },
                    PlanId = TestPlans[2].PlanId
                },
                provider, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(12);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CustomerId).Should().Be(customerId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionId).Should()
                .Be(subscriptionId);
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.SubscriptionStatus).Should()
                .Be(Subscription.StatusEnum.Active.ToString());
            result.Value.GetValueOrDefault(ChargebeeConstants.MetadataProperties.CanceledAt).Should().BeNull();
        }
    }
}

internal static class TestingExtensions
{
    public static bool IsNear(this string value, DateTime comparedTo)
    {
        return value.FromIso8601().IsNear(comparedTo, TimeSpan.FromMinutes(1));
    }
}