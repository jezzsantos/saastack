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
using Infrastructure.Shared.ApplicationServices.External;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;
using Constants = Infrastructure.Shared.ApplicationServices.External.ChargebeeStateInterpreter.Constants;
using Subscription = ChargeBee.Models.Subscription;

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

public abstract class ChargebeeHttpServiceClientSetupSpec : ExternalApiSpec
{
    private const string TestCustomerIdPrefix = "testorganizationid";
    private const string TestUserIdPrefix = "testuserid";
    protected static readonly TestPlan SetupPlan = new("SetupFee", 10M, "A setup fee", false, false, false);
    protected static readonly TestFeature TestFeature1 = new("Feature1", "A feature1");
    protected static readonly TestPlan[] TestPlans =
    [
        new TestPlan("Trial", 50M, "Trial plan", true, false, false),
        new TestPlan("Paid", 100M, "Paid plan", false, true, false),
        new TestPlan("PaidWithSetup", 100M, "PaidWithSetup plan", false, true, true)
    ];
    private static bool _isInitialized;

    protected ChargebeeHttpServiceClientSetupSpec(ExternalApiSetup setup) : base(setup)
    {
        Caller = new TestCaller();
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        ServiceClient = new ChargebeeHttpServiceClient(NoOpRecorder.Instance, settings);
        ProductFamilyId = settings.Platform.GetString(ChargebeeHttpServiceClient.ProductFamilyIdSettingName);
        if (!_isInitialized)
        {
            _isInitialized = true;
            SetupTestingSandboxAsync().GetAwaiter().GetResult();
        }
    }

    protected ICallerContext Caller { get; }

    protected string ProductFamilyId { get; }

    protected ChargebeeHttpServiceClient ServiceClient { get; }

    protected static SubscriptionBuyer CreateBuyer()
    {
        return new SubscriptionBuyer
        {
            Address = new ProfileAddress
            {
                CountryCode = CountryCodes.Default.ToString()
            },
            Subscriber = new Subscriber
            {
                EntityId = TestCustomerIdPrefix,
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

    protected async Task<(SubscriptionBuyer Buyer, Customer customer, BillingProvider Provider)> CreateCustomerAsync()
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

    private async Task SetupTestingSandboxAsync()
    {
        var caller = Caller;
#if TESTINGONLY
        // Cleanup any existing data
        var subscriptions =
            (await ServiceClient.SearchAllSubscriptionsAsync(caller, new SearchOptions(),
                CancellationToken.None))
            .Value;
        foreach (var subscription in subscriptions.Where(sub => sub.CustomerId.StartsWith(TestCustomerIdPrefix)))
        {
            (await ServiceClient.DeleteSubscriptionAsync(caller, subscription.Id, CancellationToken.None))
                .ThrowOnError();
            (await ServiceClient.DeleteCustomerAsync(caller, subscription.CustomerId, CancellationToken.None))
                .ThrowOnError();
        }

        // Cleanup any orphaned customers
        var customers =
            (await ServiceClient.SearchAllCustomersAsync(caller, new SearchOptions(), CancellationToken.None))
            .Value;
        foreach (var customer in customers.Where(c => c.Id.StartsWith(TestCustomerIdPrefix)
                                                      && c.Deleted == false))
        {
            // Ignore errors (e.g. customer has already been scheduled for delete)
            await ServiceClient.DeleteCustomerAsync(caller, customer.Id, CancellationToken.None);
        }

        var plans = (await ServiceClient.SearchAllPlansAsync(caller, new SearchOptions(), CancellationToken.None))
            .Value;
        foreach (var plan in plans)
        {
            var features =
                (await ServiceClient.SearchAllPlanFeaturesAsync(caller, plan.Id, new SearchOptions(),
                    CancellationToken.None)).Value;
            foreach (var feature in features)
            {
                (await ServiceClient.RemovePlanFeatureAsync(caller, plan.Id, feature.FeatureId,
                    CancellationToken.None)).ThrowOnError();
                (await ServiceClient.DeleteFeatureAsync(caller, feature.FeatureId, CancellationToken.None))
                    .ThrowOnError();
            }

            (await ServiceClient.DeletePlanAndPricesAsync(caller, plan.Id, CancellationToken.None)).ThrowOnError();
        }

        var charges =
            (await ServiceClient.SearchAllChargesAsync(caller, new SearchOptions(), CancellationToken.None)).Value;
        foreach (var charge in charges)
        {
            (await ServiceClient.DeleteChargeAndPricesAsync(caller, charge.Id, CancellationToken.None))
                .ThrowOnError();
        }

        // Create new test data (reactivate archived items if necessary)
        await ServiceClient.CreateProductFamilySafelyAsync(caller, ProductFamilyId, CancellationToken.None);
        var feature1 = (await ServiceClient.CreateFeatureSafelyAsync(caller, TestFeature1.Name,
            TestFeature1.Description, CancellationToken.None)).Value;
        var setupCharge = (await ServiceClient.CreateChargeSafelyAsync(caller, ProductFamilyId, SetupPlan.Name,
            SetupPlan.Description, CancellationToken.None)).Value;
        var setupChargePrice = (await ServiceClient.CreateOneOffItemPriceAsync(caller, setupCharge.Id,
            SetupPlan.Description, CurrencyCodes.Default, SetupPlan.Price, CancellationToken.None)).Value;
        foreach (var testPlan in TestPlans)
        {
            var plan = (await ServiceClient.CreatePlanSafelyAsync(caller, ProductFamilyId, testPlan.Name,
                testPlan.Description, CancellationToken.None)).Value;

            (await ServiceClient.CreateMonthlyRecurringItemPriceAsync(caller, plan.Id, testPlan.Description,
                CurrencyCodes.Default, testPlan.Price, testPlan.HasTrial, CancellationToken.None)).ThrowOnError();

            if (testPlan.HasFeature)
            {
                (await ServiceClient.AddPlanFeatureAsync(caller, plan.Id, feature1.Id, CancellationToken.None))
                    .ThrowOnError();
            }

            if (testPlan.HasSetupCharge)
            {
                (await ServiceClient.AddPlanChargeAsync(caller, plan.Id, setupChargePrice.ItemId,
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
///     customer
///     before they can subscribe to that plan. The setup fee is a one-time charge that is added to the first invoice.
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
            result.Value.Count.Should().Be(0);
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
            result.Value.Should().Contain(Constants.MetadataProperties.BillingAmount, ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(Constants.MetadataProperties.CanceledAt);
            result.Value.Should().Contain(Constants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(Constants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(Constants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(Constants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(Constants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.InTrial.ToString());
            result.Value.Should().ContainKey(Constants.MetadataProperties.TrialEnd)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
        }

        [Fact]
        public async Task WhenSubscribeExistingCustomerToTrialPlanImmediately_ThenSubscribesImmediately()
        {
            var buyer = CreateBuyer();
            (await ServiceClient.CreateCustomerAsync(Caller, buyer, CancellationToken.None)).ThrowOnError();

            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, SubscribeOptions.Immediately, CancellationToken.None);

            var endOfTrial = DateTime.UtcNow.ToNearestSecond().AddDays(7);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(11);
            result.Value.Should().Contain(Constants.MetadataProperties.BillingAmount, ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(Constants.MetadataProperties.CanceledAt);
            result.Value.Should().Contain(Constants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(Constants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(Constants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(Constants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(Constants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.InTrial.ToString());
            result.Value.Should().ContainKey(Constants.MetadataProperties.TrialEnd)
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
            result.Value.Should().Contain(Constants.MetadataProperties.BillingAmount, ToBillingAmount(TestPlans[0]));
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(Constants.MetadataProperties.CanceledAt);
            result.Value.Should().Contain(Constants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(Constants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(Constants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(endOfTrial));
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(Constants.MetadataProperties.PlanId, TestPlans[0].PlanId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(Constants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.Future.ToString());
            result.Value.Should().ContainKey(Constants.MetadataProperties.TrialEnd)
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

            result.Should().BeError(ErrorCode.PreconditionViolation,
                msg => msg.Contains("payment_method_not_present"));
        }

        [Fact]
        public async Task WhenSubscribeToPaidPlanWithPaymentSource_ThenSubscribes()
        {
            var options = SubscribeOptions.Immediately;
#if TESTINGONLY
            options.PlanId = TestPlans[1].PlanId;
#endif
            var buyer = CreateBuyer();
            var customer = (await ServiceClient.CreateCustomerAsync(Caller, buyer, CancellationToken.None)).Value;
            (await ServiceClient.CreateCustomerPaymentMethod(Caller, customer.Id, CancellationToken.None))
                .ThrowOnError();

            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, options, CancellationToken.None);

            var nextBilling = DateTime.UtcNow.ToNearestSecond().AddMonths(1);
            result.Should().BeSuccess();
            result.Value.Count.Should().Be(12);
            result.Value.Should().Contain(Constants.MetadataProperties.BillingAmount, ToBillingAmount(TestPlans[1]));
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(Constants.MetadataProperties.CanceledAt);
            result.Value.Should().Contain(Constants.MetadataProperties.CurrencyCode, CurrencyCodes.Default.Code);
            result.Value.Should().Contain(Constants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId);
            result.Value.Should().ContainKey(Constants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value => value.IsNear(nextBilling));
            result.Value.Should().Contain(Constants.MetadataProperties.PaymentMethodStatus,
                PaymentSource.StatusEnum.Valid.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.PaymentMethodType,
                Customer.CustomerPaymentMethod.TypeEnum.Card.ToString());
            result.Value.Should().Contain(Constants.MetadataProperties.PlanId, "Paid-USD-Monthly");
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(Constants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.Subscriber.EntityId);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.Active.ToString());
            result.Value.Should().NotContainKey(Constants.MetadataProperties.TrialEnd);
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
            result.Value.Count.Should().Be(1);
            result.Value[0].Id.Should().NotBeEmpty();
            result.Value[0].Amount.Should().Be(100);
            result.Value[0].Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value[0].IncludesTax.Should().BeFalse();
            result.Value[0].InvoicedOnUtc!.Value.Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value[0].LineItems.Count.Should().Be(1);
            result.Value[0].LineItems[0].Amount.Should().Be(100);
            result.Value[0].LineItems[0].Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value[0].LineItems[0].Description.Should().Be("Paid");
            result.Value[0].LineItems[0].IsTaxed.Should().BeFalse();
            result.Value[0].LineItems[0].Reference.Should().NotBeEmpty();
            result.Value[0].LineItems[0].TaxAmount.Should().Be(0);
            result.Value[0].Notes.Count.Should().Be(1);
            result.Value[0].Notes[0].Description.Should().Be("Paid plan");
            result.Value[0].Payment!.Amount.Should().Be(100);
            result.Value[0].Payment!.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Value[0].Payment!.PaidOnUtc!.Value.Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value[0].Payment!.Reference.Should().NotBeEmpty();
            result.Value[0].PeriodEndUtc!.Value.Should().BeNear(to, TimeSpan.FromMinutes(1));
            result.Value[0].PeriodStartUtc!.Value.Should().BeNear(from, TimeSpan.FromMinutes(1));
            result.Value[0].Status.Should().Be(InvoiceStatus.Paid);
            result.Value[0].TaxAmount.Should().Be(0);
        }

        //TODO: Change subscription tests
        //TODO: Cancel subscription tests
        //TODO: Transfer subscription tests
        
    }
}

internal static class TestingExtensions
{
    public static bool IsNear(this string value, DateTime comparedTo)
    {
        return value.FromIso8601().IsNear(comparedTo, TimeSpan.FromMinutes(1));
    }
}