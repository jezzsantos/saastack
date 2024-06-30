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

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

public abstract class ChargebeeHttpServiceClientSetupSpec : ExternalApiSpec
{
    private const string TestCustomerIdPrefix = "testcustomerid";
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
            CompanyReference = GenerateRandomOrganizationId(),
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
        if (planId.HasValue())
        {
            options.PlanId = planId;
        }

        var subscribed = await ServiceClient.SubscribeAsync(Caller, buyer,
            options, CancellationToken.None);
        return BillingProvider.Create(Constants.ProviderName, subscribed.Value)
            .Value;
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

        // Create new test data (reactivated archived items if necessary)
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

    private static string GenerateRandomOrganizationId()
    {
        var random = Guid.NewGuid().ToString("N").Substring(0, 16).ToLowerInvariant();
        return $"{TestCustomerIdPrefix}_{random}";
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
            monthlyPlan1.Id.Should().Be("Trial-USD-Monthly");
            monthlyPlan1.Cost.Should().Be(50M);
            monthlyPlan1.Currency.Should().Be("USD");
            monthlyPlan1.Description.Should().Be("Trial plan");
            monthlyPlan1.DisplayName.Should().Be("Trial");
            monthlyPlan1.FeatureSection.Count.Should().Be(0);
            monthlyPlan1.IsRecommended.Should().BeFalse();
            monthlyPlan1.Notes.Should().Be("Trial plan");
            monthlyPlan1.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan1.Period.Frequency.Should().Be(1);
            monthlyPlan1.SetupCost.Should().Be(0);
            monthlyPlan1.Trial!.HasTrial.Should().BeTrue();
            monthlyPlan1.Trial.Frequency.Should().Be(7);
            monthlyPlan1.Trial.Unit.Should().Be(PeriodFrequencyUnit.Day);

            var monthlyPlan2 = result.Value.Monthly[1];
            monthlyPlan2.Id.Should().Be("PaidWithSetup-USD-Monthly");
            monthlyPlan2.Cost.Should().Be(100M);
            monthlyPlan2.Currency.Should().Be("USD");
            monthlyPlan2.Description.Should().Be("PaidWithSetup plan");
            monthlyPlan2.DisplayName.Should().Be("PaidWithSetup");
            monthlyPlan2.FeatureSection.Count.Should().Be(1);
            monthlyPlan2.FeatureSection[0].Description.Should().BeNull();
            monthlyPlan2.FeatureSection[0].Features.Count.Should().Be(1);
            monthlyPlan2.FeatureSection[0].Features[0].IsIncluded.Should().BeTrue();
            monthlyPlan2.FeatureSection[0].Features[0].Description.Should().Be("A feature1");
            monthlyPlan2.IsRecommended.Should().BeFalse();
            monthlyPlan2.Notes.Should().Be("PaidWithSetup plan");
            monthlyPlan2.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan2.Period.Frequency.Should().Be(1);
            monthlyPlan2.SetupCost.Should().Be(10);
            monthlyPlan2.Trial.Should().BeNull();

            var monthlyPlan3 = result.Value.Monthly[2];
            monthlyPlan3.Id.Should().Be("Paid-USD-Monthly");
            monthlyPlan3.Cost.Should().Be(100M);
            monthlyPlan3.Currency.Should().Be("USD");
            monthlyPlan3.Description.Should().Be("Paid plan");
            monthlyPlan3.DisplayName.Should().Be("Paid");
            monthlyPlan3.FeatureSection.Count.Should().Be(1);
            monthlyPlan3.FeatureSection[0].Description.Should().BeNull();
            monthlyPlan3.FeatureSection[0].Features.Count.Should().Be(1);
            monthlyPlan3.FeatureSection[0].Features[0].IsIncluded.Should().BeTrue();
            monthlyPlan3.FeatureSection[0].Features[0].Description.Should().Be("A feature1");
            monthlyPlan3.IsRecommended.Should().BeFalse();
            monthlyPlan3.Notes.Should().Be("Paid plan");
            monthlyPlan3.Period.Unit.Should().Be(PeriodFrequencyUnit.Month);
            monthlyPlan3.Period.Frequency.Should().Be(1);
            monthlyPlan3.SetupCost.Should().Be(0);
            monthlyPlan3.Trial.Should().BeNull();
        }

        [Fact]
        public async Task WhenSearchAllInvoicesAsyncForCustomer_ThenReturnsNoInvoices()
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
        public async Task WhenSubscribeNewCustomer_ThenSubscribes()
        {
            var buyer = CreateBuyer();
            var result =
                await ServiceClient.SubscribeAsync(Caller, buyer, SubscribeOptions.Immediately, CancellationToken.None);

            var endOfTrial = DateTime.UtcNow.ToNearestSecond().AddDays(7);
            result.Should().BeSuccess();
            result.Value.Should().Contain(Constants.MetadataProperties.BillingAmount, "5000");
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodUnit, "Month");
            result.Value.Should().Contain(Constants.MetadataProperties.BillingPeriodValue, "1");
            result.Value.Should().NotContainKey(Constants.MetadataProperties.CanceledAt);
            result.Value.Should().Contain(Constants.MetadataProperties.CurrencyCode, "USD");
            result.Value.Should().Contain(Constants.MetadataProperties.CustomerId, buyer.CompanyReference);
            result.Value.Should().ContainKey(Constants.MetadataProperties.NextBillingAt)
                .WhoseValue.Should().Match(value =>
                    value.ToLong().FromUnixTimestamp().IsNear(endOfTrial, TimeSpan.FromMinutes(1)));
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodStatus);
            result.Value.Should().NotContainKey(Constants.MetadataProperties.PaymentMethodType);
            result.Value.Should().Contain(Constants.MetadataProperties.PlanId, "Trial-USD-Monthly");
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionDeleted, "False");
            result.Value.Should().ContainKey(Constants.MetadataProperties.SubscriptionId)
                .WhoseValue.Should().StartWith(buyer.CompanyReference);
            result.Value.Should().Contain(Constants.MetadataProperties.SubscriptionStatus, "InTrial");
            result.Value.Should().ContainKey(Constants.MetadataProperties.TrialEnd)
                .WhoseValue.Should().Match(value =>
                    value.ToLong().FromUnixTimestamp().IsNear(endOfTrial, TimeSpan.FromMinutes(1)));
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
            var provider = await SubscribeCustomerWithCardAsync(TestPlans[1].PlanId);
            var now = DateTime.UtcNow.ToNearestSecond();
            var from = now.SubtractDays(30).ToNearestMinute();
            var to = from.AddDays(30).ToNearestMinute();

            var result = await ServiceClient.SearchAllInvoicesAsync(Caller, provider, from, to,
                new SearchOptions(), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(1);
            result.Value[0].Id.Should().NotBeEmpty();
            result.Value[0].Amount.Should().Be(100);
            result.Value[0].Currency.Should().Be("USD");
            result.Value[0].IncludesTax.Should().BeFalse();
            result.Value[0].InvoicedOnUtc!.Value.ToUniversalTime().Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value[0].LineItems.Count.Should().Be(1);
            result.Value[0].LineItems[0].Amount.Should().Be(100);
            result.Value[0].LineItems[0].Currency.Should().Be("USD");
            result.Value[0].LineItems[0].Description.Should().Be("Paid");
            result.Value[0].LineItems[0].IsTaxed.Should().BeFalse();
            result.Value[0].LineItems[0].Reference.Should().NotBeEmpty();
            result.Value[0].LineItems[0].TaxAmount.Should().Be(0);
            result.Value[0].Notes.Count.Should().Be(1);
            result.Value[0].Notes[0].Description.Should().Be("Paid plan");
            result.Value[0].Payment!.Amount.Should().Be(100);
            result.Value[0].Payment!.Currency.Should().Be("USD");
            result.Value[0].Payment!.PaidOnUtc!.Value.ToUniversalTime().Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value[0].Payment!.Reference.Should().NotBeEmpty();
            result.Value[0].PeriodEndUtc!.Value.ToUniversalTime().Should()
                .BeNear(now.AddMonths(1), TimeSpan.FromMinutes(1));
            result.Value[0].PeriodStartUtc!.Value.ToUniversalTime().Should().BeNear(now, TimeSpan.FromMinutes(1));
            result.Value[0].Status.Should().Be(InvoiceStatus.Paid);
            result.Value[0].TaxAmount.Should().Be(0);
        }
    }
}