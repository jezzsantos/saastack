using Application.Common.Extensions;
using Application.Interfaces;
using Application.Services.Shared;
using ChargeBee.Api;
using ChargeBee.Filters.Enums;
using ChargeBee.Models;
using ChargeBee.Models.Enums;
using Common;
using Common.Configuration;
using Common.Extensions;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Shared.ApplicationServices.External;

public interface IChargebeeClient
{
    /// <summary>
    ///     Returns the added (and activated) entitlement of the feature to the plan
    /// </summary>
    Task<Result<Feature, Error>> AddFeatureEntitlementAsync(ICallerContext caller, string planId, string featureId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the added attachment of the one-time charge to the plan
    /// </summary>
    Task<Result<AttachedItem, Error>> AddOneTimeChargeAttachmentAsync(ICallerContext caller, string planId,
        string chargeId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Archives the specified item
    /// </summary>
    Task<Result<Error>> ArchiveItemAsync(ICallerContext caller, string itemId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the canceled subscription
    /// </summary>
    Task<Result<Subscription, Error>> CancelSubscriptionAsync(ICallerContext caller, string subscriptionId,
        bool endOfTerm, Optional<long> cancelAt, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the changed subscription plan
    /// </summary>
    Task<Result<Subscription, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller, string subscriptionId,
        string planId, Optional<long> trialEndsIn, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a new <see cref="Customer" /> for the specified <see cref="SubscriptionBuyer" />
    /// </summary>
    Task<Result<Customer, Error>> CreateCustomerForBuyerAsync(ICallerContext caller, string customerId,
        SubscriptionBuyer buyer, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a new <see cref="PaymentSource" /> for the specified customer
    /// </summary>
    Task<Result<PaymentSource, Error>> CreateCustomerPaymentSourceAsync(ICallerContext caller, string customerId,
        CreditCardPaymentSource card, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a new <see cref="Item" /> of the specified <see cref="Item.ItemType" />
    ///     for the specified <see cref="familyId" /> with the specified details
    /// </summary>
    Task<Result<Item, Error>> CreateItemAsync(ICallerContext caller, Item.TypeEnum type, string familyId,
        string name, string description, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new <see cref="ItemPrice" /> for the specified <see cref="itemId" /> for a
    ///     monthly-recurring charging schedule, with the specified details
    /// </summary>
    Task<Result<ItemPrice, Error>> CreateMonthlyRecurringItemPriceAsync(ICallerContext caller, string itemId,
        string description, CurrencyCodeIso4217 currency, decimal price, bool hasTrial,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new <see cref="ItemPrice" /> for the specified <see cref="itemId" /> for a
    ///     one-off charge, with the specified details
    /// </summary>
    Task<Result<ItemPrice, Error>> CreateOneOffItemPriceAsync(ICallerContext caller,
        string itemId, string description, CurrencyCodeIso4217 currency, decimal price,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new <see cref="ItemFamily" /> with the specified <see cref="familyId" />
    /// </summary>
    Task<Result<Error>> CreateProductFamilyAsync(ICallerContext caller, string familyId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a new <see cref="Subscription" /> for the specified <see cref="Customer" />
    ///     Note: AutoCollection="on" so that the subscription automatically cancels after any trial period ends (if any).
    /// </summary>
    Task<Result<Subscription, Error>> CreateSubscriptionForCustomerAsync(ICallerContext caller, string customerId,
        Subscriber subscriber, string planId, Optional<long> start, Optional<long> trialEnds,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a new <see cref="Feature.TypeEnum.Switch" /> type <see cref="Feature" /> with the specified details
    /// </summary>
    Task<Result<Feature, Error>> CreateSwitchFeatureAsync(ICallerContext caller, string name,
        string description, CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the specified customer
    /// </summary>
    Task<Result<Error>> DeleteCustomerAsync(ICallerContext caller, string customerId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the specified feature
    /// </summary>
    Task<Result<Error>> DeleteFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the specified item price
    /// </summary>
    Task<Result<Error>> DeleteItemPriceAsync(ICallerContext caller, string itemPriceId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the specified subscription
    /// </summary>
    Task<Result<Error>> DeleteSubscriptionAsync(ICallerContext caller, string subscriptionId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a customer that matches the specified <see cref="customerId" />
    /// </summary>
    Task<Result<Optional<Customer>, Error>> FindCustomerByIdAsync(ICallerContext caller, string customerId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a subscription that matches the specified <see cref="subscriptionId" />
    /// </summary>
    Task<Result<Optional<Subscription>, Error>> FindSubscriptionByIdAsync(ICallerContext caller,
        string subscriptionId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all <see cref="ItemPrice" /> for all plans, charges and addOns, that are
    ///     <see cref="ItemPrice.StatusEnum.Active" />
    /// </summary>
    Task<Result<IReadOnlyList<ItemPrice>, Error>> ListActiveItemPricesAsync(ICallerContext caller,
        string productFamilyId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the <see cref="AttachedItem" /> for charges attached to a specified plan
    /// </summary>
    Task<Result<IReadOnlyList<AttachedItem>, Error>> ListPlanChargesAsync(ICallerContext caller,
        string planId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all <see cref="Entitlement" /> for the specified plan
    /// </summary>
    Task<Result<IReadOnlyList<Entitlement>, Error>> ListPlanEntitlementsAsync(ICallerContext caller,
        string planId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the optional (switch) <see cref="Feature" />
    /// </summary>
    Task<Result<IReadOnlyList<Feature>, Error>> ListSwitchFeaturesAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a reactivated feature, that was previously archived
    /// </summary>
    Task<Result<Feature, Error>> ReactivateFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the reactivated item, that was previously archived
    /// </summary>
    Task<Result<Item, Error>> ReactivateItemAsync(ICallerContext caller, string itemId, CancellationToken none);

    /// <summary>
    ///     Returns a reactivated subscription, that may have been canceled.
    /// </summary>
    Task<Result<Subscription, Error>> ReactivateSubscriptionAsync(ICallerContext caller, string subscriptionId,
        Optional<long> trialEndsIn, CancellationToken cancellationToken);

    /// <summary>
    ///     Removes the specified <see cref="Entitlement" /> from the specified <see cref="Plan" />
    /// </summary>
    Task<Result<Error>> RemoveFeatureEntitlementAsync(ICallerContext caller, string planId, string featureId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns an (uncanceled) subscription, that may be canceling (i.e. canceled before the end of the billing period)
    /// </summary>
    Task<Result<Subscription, Error>> RemoveScheduledSubscriptionCancellationAsync(ICallerContext caller,
        string subscriptionId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all <see cref="Item" /> of the specified <see cref="type" /> that are <see cref="ItemTypeEnum.Active" />
    /// </summary>
    Task<Result<IReadOnlyList<Item>, Error>> SearchActiveItemsAsync(ICallerContext caller, Item.TypeEnum type,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all <see cref="ChargeBee.Models.Invoice" /> for the specified <see cref="customerId" />,
    ///     between the specified <see cref="fromUtc" /> and <see cref="toUtc" />,
    ///     using the specified <see cref="searchOptions" />
    /// </summary>
    Task<Result<IReadOnlyList<Invoice>, Error>> SearchAllCustomerInvoicesAsync(
        ICallerContext caller,
        string customerId, DateTime fromUtc, DateTime toUtc, SearchOptions searchOptions,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the customers
    /// </summary>
    Task<Result<IReadOnlyList<Customer>, Error>> SearchAllCustomersAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the families
    /// </summary>
    Task<Result<IReadOnlyList<ItemFamily>, Error>> SearchAllFamiliesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all <see cref="Feature" />
    /// </summary>
    Task<Result<IReadOnlyList<Feature>, Error>> SearchAllFeaturesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the item prices for the specified item
    /// </summary>
    Task<Result<IReadOnlyList<ItemPrice>, Error>> SearchAllItemPricesAsync(ICallerContext caller, string itemId,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the items of the specified <see cref="Item.TypeEnum" />
    /// </summary>
    Task<Result<IReadOnlyList<Item>, Error>> SearchAllItemsAsync(ICallerContext caller, Item.TypeEnum type,
        string familyId,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the subscriptions
    /// </summary>
    Task<Result<IReadOnlyList<Subscription>, Error>> SearchAllSubscriptionsAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the updated <see cref="Customer" />
    /// </summary>
    Task<Result<Customer, Error>> UpdateCustomerForBuyerAsync(ICallerContext caller, string customerId,
        SubscriptionBuyer buyer, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the updated billing address for the specified <see cref="Customer" />
    /// </summary>
    Task<Result<Customer, Error>> UpdateCustomerForBuyerBillingAddressAsync(ICallerContext caller,
        string customerId, SubscriptionBuyer buyer, CancellationToken cancellationToken);

    /// <summary>
    ///     Defines a credit card payment source
    /// </summary>
    public class CreditCardPaymentSource
    {
        public required string Cvv { get; init; }

        public required int ExpiryMonth { get; init; }

        public required int ExpiryYear { get; init; }

        public required string Number { get; init; }
    }
}

/// <summary>
///     Provides a service client to the Chargebee API
/// </summary>
public sealed class ChargebeeClient : IChargebeeClient
{
    private const string ApiKeySettingName = "ApplicationServices:Chargebee:ApiKey";
    private const string BaseUrlSettingName = "ApplicationServices:Chargebee:BaseUrl";
    private const string SiteNameSettingName = "ApplicationServices:Chargebee:SiteName";
    private readonly IRecorder _recorder;

    public ChargebeeClient(IRecorder recorder, IConfigurationSettings settings)
    {
        _recorder = recorder;

        var siteName = settings.Platform.GetString(SiteNameSettingName);
        var apiKey = settings.Platform.GetString(ApiKeySettingName);
        ApiConfig.Configure(siteName, apiKey);
#if TESTINGONLY
        var baseUrlOverride = settings.Platform.GetString(BaseUrlSettingName, string.Empty);
        if (baseUrlOverride.HasValue())
        {
            ApiConfig.SetBaseUrl(baseUrlOverride);
        }
#endif
    }

    public async Task<Result<Feature, Error>> AddFeatureEntitlementAsync(ICallerContext caller, string planId,
        string featureId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Entitlement.Create()
                .Action(ActionEnum.Upsert)
                .EntitlementFeatureId(0, featureId)
                .EntitlementEntityId(0, planId)
                .EntitlementEntityType(0, Entitlement.EntityTypeEnum.Plan)
                .EntitlementValue(0, "true")
                .RequestAsync();

            //Note: this client library version (3.18.1) does not seem to return an Entitlement on the result for us to return or use
            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Added entitlement of feature {Feature} to plan {Plan}", featureId, planId);

            var activated = await Feature.Activate(featureId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Activated feature {Feature} to plan {Plan}", featureId, planId);

            return activated.Feature;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Adding entitlement of feature {Feature} to plan {Plan} failed with {Code}",
                featureId, planId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<AttachedItem, Error>> AddOneTimeChargeAttachmentAsync(ICallerContext caller, string planId,
        string chargeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await AttachedItem.Create(planId)
                .ItemId(chargeId)
                .ChargeOnce(true)
                .ChargeOnEvent(ChargeOnEventEnum.SubscriptionCreation)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Added attachment {Attached} of charge {Charge} to plan {Plan}",
                result.AttachedItem.Id, chargeId, planId);
            return result.AttachedItem;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Adding attachment of charge {Charge} to plan {Plan} failed with {Code}",
                chargeId, planId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> ArchiveItemAsync(ICallerContext caller, string itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Item.Delete(itemId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Deleted item {Item}", itemId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Deleting item {Item} failed with {Code}", itemId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Subscription, Error>> CancelSubscriptionAsync(ICallerContext caller,
        string subscriptionId, bool endOfTerm, Optional<long> cancelAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = Subscription.CancelForItems(subscriptionId)
                .EndOfTerm(endOfTerm);
            if (cancelAt.HasValue)
            {
                request.CancelAt(cancelAt);
            }

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Canceled subscription {Subscription}", result.Subscription.Id);
            return result.Subscription;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Cancelling subscription {Subscription} failed with {Code}",
                subscriptionId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Subscription, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        string subscriptionId, string planId, Optional<long> trialEndsIn, CancellationToken cancellationToken)
    {
        try
        {
            var request = Subscription.UpdateForItems(subscriptionId)
                .SubscriptionItemItemPriceId(0, planId)
                .SubscriptionItemQuantity(0, 1)
                .ReplaceItemsList(true);
            if (trialEndsIn.HasValue)
            {
                request.SubscriptionItemTrialEnd(0, trialEndsIn);
            }

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Changed subscription {Subscription} to plan {Plan}", result.Subscription.Id,
                planId);
            return result.Subscription;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Changing subscription {Subscription} to plan {Plan} failed with {Code}",
                subscriptionId,
                planId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Customer, Error>> CreateCustomerForBuyerAsync(ICallerContext caller, string customerId,
        SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        try
        {
            var subscriberType = $"{buyer.Subscriber.EntityType}Id";
            var subscriberId = buyer.Subscriber.EntityId;

            var request = Customer.Create()
                .Id(customerId)
                .FirstName(buyer.Name.FirstName)
                .LastName(buyer.Name.LastName)
                .Email(buyer.EmailAddress)
                .Phone(buyer.PhoneNumber)
                .Company(buyer.GetSubscriberId(customerId))
                .BillingAddressFirstName(buyer.Name.FirstName)
                .BillingAddressLastName(buyer.Name.LastName)
                .BillingAddressEmail(buyer.EmailAddress)
                .BillingAddressLine1(buyer.Address.Line1)
                .BillingAddressLine2(buyer.Address.Line2)
                .BillingAddressLine3(buyer.Address.Line3)
                .BillingAddressCity(buyer.Address.City)
                .BillingAddressState(buyer.Address.State)
                .BillingAddressZip(buyer.Address.Zip)
                .BillingAddressCountry(CountryCodes.FindOrDefault(buyer.Address.CountryCode).Alpha2)
                .MetaData(JToken.FromObject(new Dictionary<string, string>
                {
                    { subscriberType, subscriberId },
                    { ChargebeeHttpServiceClient.BuyerMetadataId, buyer.Id }
                }));

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(), "Chargebee Client: Created new customer {Customer}",
                result.Customer.Id);
            return result.Customer;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating customer {Customer} failed with {Code}", customerId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<PaymentSource, Error>> CreateCustomerPaymentSourceAsync(ICallerContext caller,
        string customerId, IChargebeeClient.CreditCardPaymentSource card, CancellationToken cancellationToken)
    {
        try
        {
            var request = await PaymentSource.CreateCard()
                .CustomerId(customerId)
                .CardNumber(card.Number)
                .CardCvv(card.Cvv)
                .CardExpiryMonth(card.ExpiryMonth)
                .CardExpiryYear(card.ExpiryYear)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created card payment source for customer {Customer}", customerId);
            return request.PaymentSource;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating card payment source for customer {Customer} failed with {Code}",
                customerId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Item, Error>> CreateItemAsync(ICallerContext caller, Item.TypeEnum type,
        string familyId, string name, string description,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await Item.Create()
                .Id(name)
                .Description(description)
                .Name(name)
                .ItemFamilyId(familyId)
                .Type(type)
                .EnabledInPortal(true)
                .EnabledForCheckout(true)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created {Type} item for {Family} with name {Name}", type, familyId, name);
            return result.Item;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating {Type} item for family {Family} with name {Name} failed with {Code}",
                type, familyId, name,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<ItemPrice, Error>> CreateMonthlyRecurringItemPriceAsync(ICallerContext caller,
        string itemId, string description, CurrencyCodeIso4217 currency, decimal price, bool hasTrial,
        CancellationToken cancellationToken)
    {
        var id = $"{itemId}-{currency.Code}-Monthly";
        var name = $"{itemId} {currency.Code} Monthly";
        var priceInCurrency = CurrencyCodes.ToMinorUnit(currency, price);

        try
        {
            var request = ItemPrice.Create()
                .Id(id)
                .ItemId(itemId)
                .Name(name)
                .PricingModel(PricingModelEnum.FlatFee)
                .Price(priceInCurrency)
                .Period(1)
                .PeriodUnit(ItemPrice.PeriodUnitEnum.Month)
                .ExternalName(itemId)
                .Description(description)
                .ShowDescriptionInInvoices(true)
                .InvoiceNotes(description)
                .ShowDescriptionInQuotes(true);

            if (hasTrial)
            {
                request.TrialPeriod(7)
                    .TrialPeriodUnit(ItemPrice.TrialPeriodUnitEnum.Day);
            }

            var result = await request
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created monthly-recurring item price for {Item}", itemId);
            return result.ItemPrice;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating monthly-recurring item price for item {Item} failed with {Code}", itemId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<ItemPrice, Error>> CreateOneOffItemPriceAsync(ICallerContext caller,
        string itemId, string description, CurrencyCodeIso4217 currency, decimal price,
        CancellationToken cancellationToken)
    {
        var id = $"{itemId}-{currency.Code}";
        var name = $"{itemId} {currency.Code}";
        var priceInCurrency = CurrencyCodes.ToMinorUnit(currency, price);

        try
        {
            var result = await ItemPrice.Create()
                .Id(id)
                .ItemId(itemId)
                .Name(name)
                .PricingModel(PricingModelEnum.FlatFee)
                .Price(priceInCurrency)
                .ExternalName(itemId)
                .Description(description)
                .ShowDescriptionInInvoices(true)
                .InvoiceNotes(description)
                .ShowDescriptionInQuotes(true)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created one-off item price for {Item}", itemId);
            return result.ItemPrice;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating one-off item price for item {Item} failed with {Code}", itemId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> CreateProductFamilyAsync(ICallerContext caller, string familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            await ItemFamily.Create()
                .Id(familyId)
                .Name(familyId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created item family {Family}", familyId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating item family {Family} failed with {Code}", familyId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Subscription, Error>> CreateSubscriptionForCustomerAsync(ICallerContext caller,
        string customerId, Subscriber subscriber, string planId, Optional<long> start,
        Optional<long> trialEnds, CancellationToken cancellationToken)
    {
        try
        {
            var subscriberType = $"{subscriber.EntityType}Id";
            var subscriberId = subscriber.EntityId;
            var subscriptionId = customerId.MakeSubscriptionId();
            var request = Subscription.CreateWithItems(customerId)
                .Id(subscriptionId)
                .AutoCollection(AutoCollectionEnum.On)
                .SubscriptionItemItemPriceId(0, planId)
                .SubscriptionItemQuantity(0, 1)
                .MetaData(JToken.FromObject(new Dictionary<string, string>
                {
                    { subscriberType, subscriberId }
                }));
            if (trialEnds.HasValue)
            {
                request.SubscriptionItemTrialEnd(0, trialEnds.Value);
            }

            if (start.HasValue)
            {
                request.StartDate(start.Value);
            }

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created new subscription {Subscription} for customer {Customer}",
                result.Subscription.Id, customerId);
            return result.Subscription;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating subscription for customer {Customer} failed with {Code}",
                customerId, ex.ApiErrorCode);
            var error = ChargebeeError(ex);

            if (ex.ApiErrorCode == "payment_method_not_present")
            {
                return Error.PreconditionViolation(error.Message);
            }

            return error;
        }
    }

    public async Task<Result<Feature, Error>> CreateSwitchFeatureAsync(ICallerContext caller, string name,
        string description,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await Feature.Create()
                .Name(name)
                .Description(description)
                .Type(Feature.TypeEnum.Switch)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Created new feature {Feature}", result.Feature.Id);
            return result.Feature;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Creating feature failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> DeleteCustomerAsync(ICallerContext caller, string customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Customer.Delete(customerId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Deleted customer {Customer}", customerId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Deleting customer {Customer} failed with {Code}", customerId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> DeleteFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Feature.Archive(featureId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Archived feature {Feature}", featureId);

            await Feature.Delete(featureId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Deleted feature {Feature}", featureId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Archiving and deleting feature {Feature} failed with {Code}", featureId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> DeleteItemPriceAsync(ICallerContext caller, string itemPriceId,
        CancellationToken cancellationToken)
    {
        try
        {
            await ItemPrice.Delete(itemPriceId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Deleted item price {Price}", itemPriceId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Deleting item price {Price} failed with {Code}", itemPriceId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> DeleteSubscriptionAsync(ICallerContext caller, string subscriptionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Subscription.Delete(subscriptionId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Deleted subscription {Subscription}", subscriptionId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Deleting subscription {Subscription} failed with {Code}", subscriptionId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Optional<Customer>, Error>> FindCustomerByIdAsync(ICallerContext caller,
        string customerId, CancellationToken cancellationToken)
    {
        try
        {
            var request = await Customer.List()
                .Id()
                .Is(customerId)
                .Limit(1)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for customer {Customer}, and found {Count}", customerId,
                request.List.Count);
            return request.List.HasNone()
                ? Optional<Customer>.None
                : request.List.First().Customer.ToOptional();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for customer {Customer} failed with {Code}", customerId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Optional<Subscription>, Error>> FindSubscriptionByIdAsync(ICallerContext caller,
        string subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var request = await Subscription.List()
                .Id()
                .Is(subscriptionId)
                .Limit(1)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for subscription {Subscription}, and found {Count}", subscriptionId,
                request.List.Count);
            return request.List.HasNone()
                ? Optional<Subscription>.None
                : request.List.First().Subscription.ToOptional();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for subscription {Subscription} failed with {Code}",
                subscriptionId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ItemPrice>, Error>> ListActiveItemPricesAsync(ICallerContext caller,
        string productFamilyId, CancellationToken cancellationToken)
    {
        try
        {
            var request = await ItemPrice.List()
                .Status().Is(ItemPrice.StatusEnum.Active)
                .ItemFamilyId().Is(productFamilyId)
                .Limit(100)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for active item prices for family {ProductFamily}, and found {Count}",
                request.List.Count, productFamilyId);
            return request.List.Select(entry => entry.ItemPrice).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for active item prices for family {ProductFamily} failed with {Code}",
                productFamilyId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<AttachedItem>, Error>> ListPlanChargesAsync(ICallerContext caller,
        string planId, CancellationToken cancellationToken)
    {
        try
        {
            var request = await AttachedItem.List(planId)
                .ItemType().Is(ItemTypeEnum.Charge)
                .Limit(100)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for charges of plan {Plan}, and found {Count}", planId,
                request.List.Count);
            return request.List.Select(entry => entry.AttachedItem).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for charges of plan {Plan} failed with {Code}", planId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Entitlement>, Error>> ListPlanEntitlementsAsync(ICallerContext caller,
        string planId, CancellationToken cancellationToken)
    {
        try
        {
            var request = await Entitlement.List()
                .EntityType().Is(Entitlement.EntityTypeEnum.Plan)
                .EntityId().Is(planId)
                .Limit(100)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for entitlements of plan {Plan}, and found {Count}", planId,
                request.List.Count);
            return request.List.Select(entry => entry.Entitlement).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for entitlements of plan {Plan} failed with {Code}", planId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Feature>, Error>> ListSwitchFeaturesAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await Feature.List()
                .Status().Is(Feature.StatusEnum.Active)
                .Type().Is(Feature.TypeEnum.Switch)
                .Limit(100)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for active switch features, and found {Count}", request.List.Count);
            return request.List.Select(entry => entry.Feature).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for active switch features  failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Feature, Error>> ReactivateFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await Feature.Reactivate(featureId)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Reactivated feature {Feature}", result.Feature.Id);
            return result.Feature;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Reactivating feature {Feature} failed with {Code}", featureId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Item, Error>> ReactivateItemAsync(ICallerContext caller, string itemId,
        CancellationToken none)
    {
        try
        {
            var result = await Item.Update(itemId)
                .Status(Item.StatusEnum.Active)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Reactivated item {Item}", result.Item.Id);
            return result.Item;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Reactivating item {Item} failed with {Code}", itemId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Subscription, Error>> ReactivateSubscriptionAsync(ICallerContext caller,
        string subscriptionId, Optional<long> trialEndsIn, CancellationToken cancellationToken)
    {
        try
        {
            var request = Subscription.Reactivate(subscriptionId);
            if (trialEndsIn.HasValue)
            {
                request.TrialEnd(trialEndsIn);
            }

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Reactivated subscription {Subscription}", result.Subscription.Id);
            return result.Subscription;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Reactivating subscription {Subscription} failed with {Code}", subscriptionId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Error>> RemoveFeatureEntitlementAsync(ICallerContext caller, string planId,
        string featureId, CancellationToken cancellationToken)
    {
        try
        {
            await Entitlement.Create()
                .Action(ActionEnum.Remove)
                .EntitlementFeatureId(0, featureId)
                .EntitlementEntityId(0, planId)
                .EntitlementEntityType(0, Entitlement.EntityTypeEnum.Plan)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Removed feature entitlement {Feature} from plan {Plan}", featureId, planId);
            return Result.Ok;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Removing feature entitlement {Feature} from plan {Plan} failed with {Code}",
                featureId, planId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Subscription, Error>> RemoveScheduledSubscriptionCancellationAsync(
        ICallerContext caller, string subscriptionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = Subscription.RemoveScheduledCancellation(subscriptionId);

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Removed cancellation on subscription {Subscription}", result.Subscription.Id);
            return result.Subscription;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Removing cancellation on subscription {Subscription} failed with {Code}",
                subscriptionId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Item>, Error>> SearchActiveItemsAsync(ICallerContext caller,
        Item.TypeEnum type, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Item.List()
                .Type().Is(type)
                .Status().Is(Item.StatusEnum.Active)
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all items of type {Type}, and found {Count}", type,
                request.List.Count);
            return request.List.Select(entry => entry.Item).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all items of type {Type} failed with {Code}", type,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Invoice>, Error>> SearchAllCustomerInvoicesAsync(
        ICallerContext caller, string customerId, DateTime fromUtc, DateTime toUtc,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Invoice.List()
                .CustomerId().Is(customerId)
                .Date().Between(fromUtc, toUtc)
                .Limit(limit)
                .SortByDate(SortOrderEnum.Asc)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all invoices for {Customer}, and found {Count}", customerId,
                request.List.Count);
            return request.List.Select(entry => entry.Invoice).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all invoices for {Customer} failed with {Code}", customerId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Customer>, Error>> SearchAllCustomersAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Customer.List()
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all customers, and found {Count}", request.List.Count);
            return request.List.Select(entry => entry.Customer).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all customers failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ItemFamily>, Error>> SearchAllFamiliesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await ItemFamily.List()
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all item families, and found {Count}", request.List.Count);
            return request.List.Select(entry => entry.ItemFamily).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all item families failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Feature>, Error>> SearchAllFeaturesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Feature.List()
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all features, and found {Count}", request.List.Count);
            return request.List.Select(entry => entry.Feature).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all features failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ItemPrice>, Error>> SearchAllItemPricesAsync(ICallerContext caller,
        string itemId, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await ItemPrice.List()
                .ItemId().Is(itemId)
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all item prices for item {Item}, and found {Count}", itemId,
                request.List.Count);
            return request.List.Select(entry => entry.ItemPrice).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all item prices of item {Item} failed with {Code}", itemId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Item>, Error>> SearchAllItemsAsync(ICallerContext caller, Item.TypeEnum type,
        string familyId, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Item.List()
                .ItemFamilyId().Is(familyId)
                .Type().Is(type)
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all items of type {Type}, and found {Count}", type, request.List.Count);
            return request.List.Select(entry => entry.Item).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all items of type {Type} failed with {Code}", type, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<Subscription>, Error>> SearchAllSubscriptionsAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        try
        {
            var limit = searchOptions.Limit != 0
                ? searchOptions.Limit
                : 100;
            var request = await Subscription.List()
                .Limit(limit)
                .RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Searched for all subscriptions, and found {Count}", request.List.Count);
            return request.List.Select(entry => entry.Subscription).ToList();
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Searching for all subscriptions failed with {Code}", ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Customer, Error>> UpdateCustomerForBuyerAsync(ICallerContext caller, string customerId,
        SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        try
        {
            var subscriberType = $"{buyer.Subscriber.EntityType}Id";
            var subscriberId = buyer.Subscriber.EntityId;

            var request = Customer.Update(customerId)
                .FirstName(buyer.Name.FirstName)
                .LastName(buyer.Name.LastName)
                .Email(buyer.EmailAddress)
                .Phone(buyer.PhoneNumber)
                .Company(buyer.GetSubscriberId(customerId))
                .MetaData(JToken.FromObject(new Dictionary<string, string>
                {
                    { subscriberType, subscriberId },
                    { ChargebeeHttpServiceClient.BuyerMetadataId, buyer.Id }
                }));

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(), "Chargebee Client: Updated customer {Customer}",
                result.Customer.Id);
            return result.Customer;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Updating customer {Customer} failed with {Code}", customerId, ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    public async Task<Result<Customer, Error>> UpdateCustomerForBuyerBillingAddressAsync(ICallerContext caller,
        string customerId, SubscriptionBuyer buyer, CancellationToken cancellationToken)
    {
        try
        {
            var request = Customer.UpdateBillingInfo(customerId)
                .BillingAddressFirstName(buyer.Name.FirstName)
                .BillingAddressLastName(buyer.Name.LastName)
                .BillingAddressEmail(buyer.EmailAddress)
                .BillingAddressLine1(buyer.Address.Line1)
                .BillingAddressLine2(buyer.Address.Line2)
                .BillingAddressLine3(buyer.Address.Line3)
                .BillingAddressCity(buyer.Address.City)
                .BillingAddressState(buyer.Address.State)
                .BillingAddressZip(buyer.Address.Zip)
                .BillingAddressCountry(CountryCodes.FindOrDefault(buyer.Address.CountryCode).Alpha2);

            var result = await request.RequestAsync();

            _recorder.TraceDebug(caller.ToCall(),
                "Chargebee Client: Updated customer billing {Customer} billing address", customerId);
            return result.Customer;
        }
        catch (ApiException ex)
        {
            _recorder.TraceError(caller.ToCall(),
                "Chargebee Client: Updating customer billing {Customer} failed with {Code}", customerId,
                ex.ApiErrorCode);
            return ChargebeeError(ex);
        }
    }

    private static Error ChargebeeError(ApiException ex)
    {
        var message = $"Chargebee failed with error: {ex.Message}, and code: {ex.ApiErrorCode}";
        return Error.Unexpected(message);
    }
}