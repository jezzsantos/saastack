#if TESTINGONLY
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Services.Shared;
using ChargeBee.Models;
using Common;
using Common.Extensions;

namespace Infrastructure.External.ApplicationServices;

/// <inheritdoc cref="ChargebeeHttpServiceClient" />
partial class ChargebeeHttpServiceClient
{
    public async Task<Result<Error>> AddPlanChargeAsync(ICallerContext caller, string planId, string chargeId,
        CancellationToken cancellationToken)
    {
        var added = await _serviceClient.AddOneTimeChargeAttachmentAsync(caller, planId, chargeId, cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Attached Chargebee charge {Charge} to plan {Plan}", chargeId,
            planId);
        return Result.Ok;
    }

    public async Task<Result<Error>> AddPlanFeatureAsync(ICallerContext caller, string planId, string featureId,
        CancellationToken cancellationToken)
    {
        var added = await _serviceClient.AddFeatureEntitlementAsync(caller, planId, featureId, cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Added Chargebee feature {Feature} to plan {Plan}", featureId,
            planId);
        return Result.Ok;
    }

    public async Task<Result<Item, Error>> CreateChargeSafelyAsync(ICallerContext caller, string familyId, string name,
        string description, CancellationToken cancellationToken)
    {
        var existingCharges =
            (await _serviceClient.SearchAllItemsAsync(caller, Item.TypeEnum.Charge, familyId, new SearchOptions(),
                CancellationToken.None)).Value;
        var charge = existingCharges.FirstOrDefault(charge => charge.Id == name);
        if (charge.Exists())
        {
            if (charge.Status == Item.StatusEnum.Archived)
            {
                var reactivated =
                    await _serviceClient.ReactivateItemAsync(caller, charge.Id, CancellationToken.None);
                if (reactivated.IsFailure)
                {
                    return reactivated.Error;
                }

                _recorder.TraceInformation(caller.ToCall(), "Reactivated Chargebee charge {Charge}",
                    reactivated.Value.Id);
                return reactivated.Value;
            }

            _recorder.TraceInformation(caller.ToCall(), "Chargebee charge {Charge} exists", charge.Id);
            return charge;
        }

        var created = await _serviceClient.CreateItemAsync(caller, Item.TypeEnum.Charge, familyId, name, description,
            cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee charge {Charge}", created.Value.Id);
        return created.Value;
    }

    public async Task<Result<Customer, Error>> CreateCustomerAsync(ICallerContext caller, SubscriptionBuyer buyer,
        CancellationToken cancellationToken)
    {
        var customerId = buyer.MakeCustomerId();
        var created = await _serviceClient.CreateCustomerForBuyerAsync(caller, customerId, buyer, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee customer {Customer}", customerId);
        return created.Value;
    }

    public async Task<Result<PaymentSource, Error>> CreateCustomerPaymentMethod(ICallerContext caller,
        string customerId,
        CancellationToken cancellationToken)
    {
        var created =
            await _serviceClient.CreateCustomerPaymentSourceAsync(caller, customerId,
                ChargebeeStateInterpreter.Constants.TestCard, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee payment method for customer {Customer}",
            customerId);
        return created.Value;
    }

    public async Task<Result<Feature, Error>> CreateFeatureSafelyAsync(ICallerContext caller, string name,
        string description, CancellationToken cancellationToken)
    {
        var existingFeatures =
            (await _serviceClient.SearchAllFeaturesAsync(caller, new SearchOptions(), CancellationToken.None)).Value;
        var feature = existingFeatures.FirstOrDefault(feature => feature.Name == name);
        if (feature.Exists())
        {
            if (feature.Status == Feature.StatusEnum.Archived)
            {
                var reactivated =
                    await _serviceClient.ReactivateFeatureAsync(caller, feature.Id, CancellationToken.None);
                if (reactivated.IsFailure)
                {
                    return reactivated.Error;
                }

                _recorder.TraceInformation(caller.ToCall(), "Reactivated Chargebee switch feature {Feature}",
                    reactivated.Value.Id);
                return reactivated.Value;
            }

            _recorder.TraceInformation(caller.ToCall(), "Chargebee switch feature {Feature} exists", feature.Id);
            return feature;
        }

        var created = await _serviceClient.CreateSwitchFeatureAsync(caller, name, description, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee switch feature {Feature}", created.Value.Id);
        return created.Value;
    }

    public async Task<Result<ItemPrice, Error>> CreateMonthlyRecurringItemPriceAsync(ICallerContext caller,
        string itemId, string description, CurrencyCodeIso4217 currency, decimal price, bool hasTrial,
        CancellationToken cancellationToken)
    {
        var created =
            await _serviceClient.CreateMonthlyRecurringItemPriceAsync(caller, itemId, description, currency, price,
                hasTrial, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee monthly-recurring item price for item {Item}",
            itemId);

        return created.Value;
    }

    public async Task<Result<ItemPrice, Error>> CreateOneOffItemPriceAsync(ICallerContext caller, string itemId,
        string description, CurrencyCodeIso4217 currency, decimal price, CancellationToken cancellationToken)
    {
        var created =
            await _serviceClient.CreateOneOffItemPriceAsync(caller, itemId, description, currency, price,
                cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee one-off item price for item {Item}", itemId);
        return created.Value;
    }

    public async Task<Result<Item, Error>> CreatePlanSafelyAsync(ICallerContext caller, string familyId, string name,
        string description, CancellationToken cancellationToken)
    {
        var existingPlans =
            (await _serviceClient.SearchAllItemsAsync(caller, Item.TypeEnum.Plan, familyId, new SearchOptions(),
                CancellationToken.None)).Value;
        var plan = existingPlans.FirstOrDefault(charge => charge.Id == name);
        if (plan.Exists())
        {
            if (plan.Status == Item.StatusEnum.Archived)
            {
                var reactivated =
                    await _serviceClient.ReactivateItemAsync(caller, plan.Id, CancellationToken.None);
                if (reactivated.IsFailure)
                {
                    return reactivated.Error;
                }

                _recorder.TraceInformation(caller.ToCall(), "Reactivated Chargebee plan {Plan}",
                    reactivated.Value.Id);
                return reactivated.Value;
            }

            _recorder.TraceInformation(caller.ToCall(), "Chargebee plan {Plan} exists", plan.Id);
            return plan;
        }

        var created = await _serviceClient.CreateItemAsync(caller, Item.TypeEnum.Plan, familyId, name, description,
            cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee plan {Plan}", created.Value.Id);
        return created.Value;
    }

    public async Task<Result<Error>> CreateProductFamilySafelyAsync(ICallerContext caller, string familyId,
        CancellationToken cancellationToken)
    {
        var families = (await _serviceClient.SearchAllFamiliesAsync(caller, new SearchOptions(),
            CancellationToken.None)).Value;
        if (families.Any(f => f.Id == _productFamilyId))
        {
            return Result.Ok;
        }

        var created = await _serviceClient.CreateProductFamilyAsync(caller, familyId, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created Chargebee product family {Family}", familyId);
        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteChargeAndPricesAsync(ICallerContext caller, string chargeId,
        CancellationToken cancellationToken)
    {
        var retrievedPrices =
            await _serviceClient.SearchAllItemPricesAsync(caller, chargeId, new SearchOptions(), cancellationToken);
        if (retrievedPrices.IsFailure)
        {
            return retrievedPrices.Error;
        }

        var prices = retrievedPrices.Value;
        foreach (var price in prices)
        {
            var deletedPrice = await _serviceClient.DeleteItemPriceAsync(caller, price.Id, cancellationToken);
            if (deletedPrice.IsFailure)
            {
                return deletedPrice.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee item price {Price} for item {Item}",
                price.Id, chargeId);
        }

        var archived = await _serviceClient.ArchiveItemAsync(caller, chargeId, cancellationToken);
        if (archived.IsFailure)
        {
            return archived.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee charge {Item}", chargeId);
        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteCustomerAsync(ICallerContext caller, string customerId,
        CancellationToken cancellationToken)
    {
        var deleted = await _serviceClient.DeleteCustomerAsync(caller, customerId, cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee customer {Customer}", customerId);

        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken)
    {
        var deleted = await _serviceClient.DeleteFeatureAsync(caller, featureId, cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee feature {Feature}", featureId);
        return Result.Ok;
    }

    public async Task<Result<Error>> DeletePlanAndPricesAsync(ICallerContext caller, string planId,
        CancellationToken cancellationToken)
    {
        var retrievedPrices =
            await _serviceClient.SearchAllItemPricesAsync(caller, planId, new SearchOptions(), cancellationToken);
        if (retrievedPrices.IsFailure)
        {
            return retrievedPrices.Error;
        }

        var prices = retrievedPrices.Value;
        foreach (var price in prices)
        {
            var deletedPrice = await _serviceClient.DeleteItemPriceAsync(caller, price.Id, cancellationToken);
            if (deletedPrice.IsFailure)
            {
                return deletedPrice.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee item price {Price} for item {Item}",
                price.Id, planId);
        }

        var archived = await _serviceClient.ArchiveItemAsync(caller, planId, cancellationToken);
        if (archived.IsFailure)
        {
            return archived.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee plan {Item}", planId);
        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteSubscriptionAsync(ICallerContext caller, string subscriptionId,
        CancellationToken cancellationToken)
    {
        var deleted = await _serviceClient.DeleteSubscriptionAsync(caller, subscriptionId, cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Deleted Chargebee subscription {Subscription}", subscriptionId);
        return Result.Ok;
    }

    public async Task<Result<Feature, Error>> ReactivateFeatureAsync(ICallerContext caller, string featureId,
        CancellationToken cancellationToken)
    {
        var restored =
            await _serviceClient.ReactivateFeatureAsync(caller, featureId, cancellationToken);
        if (restored.IsFailure)
        {
            return restored.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee subscriptions");
        return new Result<Feature, Error>(restored.Value);
    }

    public async Task<Result<Error>> RemovePlanFeatureAsync(ICallerContext caller, string planId, string featureId,
        CancellationToken cancellationToken)
    {
        var removed = await _serviceClient.RemoveFeatureEntitlementAsync(caller, planId, featureId, cancellationToken);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Removed Chargebee feature {Feature} from plan {Plan}", featureId,
            planId);
        return Result.Ok;
    }

    public async Task<Result<IReadOnlyList<Item>, Error>> SearchAllChargesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedCharges =
            await _serviceClient.SearchActiveItemsAsync(caller, Item.TypeEnum.Charge, searchOptions, cancellationToken);
        if (retrievedCharges.IsFailure)
        {
            return retrievedCharges.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee charges");
        return new Result<IReadOnlyList<Item>, Error>(retrievedCharges.Value);
    }

    public async Task<Result<IReadOnlyList<Customer>, Error>> SearchAllCustomersAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedCustomers =
            await _serviceClient.SearchAllCustomersAsync(caller, searchOptions, cancellationToken);
        if (retrievedCustomers.IsFailure)
        {
            return retrievedCustomers.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee customers");
        return new Result<IReadOnlyList<Customer>, Error>(retrievedCustomers.Value);
    }

    public async Task<Result<IReadOnlyList<ItemFamily>, Error>> SearchAllFamiliesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedFamilies =
            await _serviceClient.SearchAllFamiliesAsync(caller, searchOptions, cancellationToken);
        if (retrievedFamilies.IsFailure)
        {
            return retrievedFamilies.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee families");
        return new Result<IReadOnlyList<ItemFamily>, Error>(retrievedFamilies.Value);
    }

    public async Task<Result<IReadOnlyList<Feature>, Error>> SearchAllFeaturesAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedFeatures =
            await _serviceClient.SearchAllFeaturesAsync(caller, searchOptions, cancellationToken);
        if (retrievedFeatures.IsFailure)
        {
            return retrievedFeatures.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee features");
        return new Result<IReadOnlyList<Feature>, Error>(retrievedFeatures.Value);
    }

    public async Task<Result<IReadOnlyList<Entitlement>, Error>> SearchAllPlanFeaturesAsync(ICallerContext caller,
        string planId, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedEntitlements =
            await _serviceClient.ListPlanEntitlementsAsync(caller, planId, cancellationToken);
        if (retrievedEntitlements.IsFailure)
        {
            return retrievedEntitlements.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee plan features");
        return new Result<IReadOnlyList<Entitlement>, Error>(retrievedEntitlements.Value);
    }

    public async Task<Result<IReadOnlyList<Item>, Error>> SearchAllPlansAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedPlans =
            await _serviceClient.SearchActiveItemsAsync(caller, Item.TypeEnum.Plan, searchOptions, cancellationToken);
        if (retrievedPlans.IsFailure)
        {
            return retrievedPlans.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee plans");
        return new Result<IReadOnlyList<Item>, Error>(retrievedPlans.Value);
    }

    public async Task<Result<IReadOnlyList<Subscription>, Error>> SearchAllSubscriptionsAsync(ICallerContext caller,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var retrievedSubscriptions =
            await _serviceClient.SearchAllSubscriptionsAsync(caller, searchOptions, cancellationToken);
        if (retrievedSubscriptions.IsFailure)
        {
            return retrievedSubscriptions.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all Chargebee subscriptions");
        return new Result<IReadOnlyList<Subscription>, Error>(retrievedSubscriptions.Value);
    }
}
#endif