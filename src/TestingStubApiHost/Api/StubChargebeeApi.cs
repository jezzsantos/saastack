using ChargeBee.Models;
using ChargeBee.Models.Enums;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

namespace TestingStubApiHost.Api;

[BaseApiFrom("/chargebee")]
public sealed class StubChargebeeApi : StubApiBase
{
    private const string ItemFamilyId = "afamilyid";
    private static readonly List<ChargebeeCustomer> Customers = [];
    private static readonly List<ChargebeeSubscription> Subscriptions = [];
    private static readonly TimeSpan TrialPeriod = TimeSpan.FromDays(14);

    public StubChargebeeApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiPostResult<string, ChargebeeCancelSubscriptionResponse>> CancelSubscription(
        ChargebeeCancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Cancelling Subscription Plan via Chargebee: for: {Subscription}",
            request.Id!);

        var subscription = Subscriptions.Find(s => s.Id == request.Id);
        if (subscription.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Subscription {request.Id} not found");
        }

        var customer = Customers.Find(c => c.Id == subscription.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {subscription.CustomerId} not found");
        }

        if (request.EndOfTerm)
        {
            subscription.CancelledAt = subscription.NextBillingAt;
            subscription.Status = Subscription.StatusEnum.Cancelled.ToString(true);
        }

        return () =>
            new PostResult<ChargebeeCancelSubscriptionResponse>(new ChargebeeCancelSubscriptionResponse
            {
                Customer = customer,
                Subscription = subscription
            });
    }

    public async Task<ApiPostResult<string, ChargebeeChangeSubscriptionPlanResponse>> ChangeSubscription(
        ChargebeeChangeSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var firstItem = request.SubscriptionItems.Single();

        Recorder.TraceInformation(null,
            "StubChargebee: Changing Subscription Plan via Chargebee: for: {Subscription} and {Plan}",
            request.Id!, firstItem.ItemPriceId!);

        var subscription = Subscriptions.Find(s => s.Id == request.Id);
        if (subscription.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Subscription {request.Id} not found");
        }

        var customer = Customers.Find(c => c.Id == subscription.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {subscription.CustomerId} not found");
        }

        if (request.ReplaceItemsList)
        {
            subscription.SubscriptionItems.Clear();
            subscription.SubscriptionItems.AddRange(request.SubscriptionItems);
        }

        subscription.Deleted = null;
        subscription.CancelledAt = null;

        return () =>
            new PostResult<ChargebeeChangeSubscriptionPlanResponse>(new ChargebeeChangeSubscriptionPlanResponse
            {
                Customer = customer,
                Subscription = subscription
            });
    }

    public async Task<ApiPostResult<string, ChargebeeCreateCustomerResponse>> CreateCustomer(
        ChargebeeCreateCustomerRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Creating Customer via Chargebee: with {Id}: for: {FirstName} {LastName}, and {EmailAddress}",
            request.Id!, request.FirstName!, request.LastName!, request.Email!);
        var customer = new ChargebeeCustomer
        {
            Id = $"cus_{GenerateRandomIdentifier()}",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone
        };
        Customers.Add(customer);

        return () =>
            new PostResult<ChargebeeCreateCustomerResponse>(new ChargebeeCreateCustomerResponse
            {
                Customer = customer
            });
    }

    public async Task<ApiPostResult<string, ChargebeeCreateSubscriptionResponse>> CreateSubscription(
        ChargebeeCreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var firstItem = request.SubscriptionItems.Single();
        var isTrialPlan = firstItem.ItemPriceId!.Contains("trial", StringComparison.InvariantCultureIgnoreCase);

        Recorder.TraceInformation(null,
            "StubChargebee: Creating Subscription via Chargebee: for: {Customer} and {Plan}",
            request.CustomerId!, firstItem.ItemPriceId);

        var customer = Customers.Find(c => c.Id == request.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {request.CustomerId} not found");
        }

        var subscription = new ChargebeeSubscription
        {
            Id = $"sub_{GenerateRandomIdentifier()}",
            BillingPeriod = 1,
            BillingPeriodUnit = PeriodUnitEnum.Month.ToString(true),
            CurrencyCode = CurrencyCodes.Default.Code,
            CustomerId = customer.Id,
            NextBillingAt = isTrialPlan
                ? DateTime.UtcNow.Add(TrialPeriod).ToUnixSeconds()
                : DateTime.UtcNow.AddMonths(1).ToUnixSeconds(),
            Status = isTrialPlan
                ? Subscription.StatusEnum.InTrial.ToString(true)
                : Subscription.StatusEnum.Future.ToString(true),
            SubscriptionItems =
            [
                new ChargebeeSubscriptionItem
                {
                    ItemPriceId = firstItem.ItemPriceId,
                    ItemType = firstItem.ItemType,
                    UnitPrice = firstItem.UnitPrice,
                    Quantity = firstItem.Quantity,
                    Amount = firstItem.Amount,
                    TrialEnd = firstItem.TrialEnd
                }
            ],
            TrialEnd = isTrialPlan
                ? DateTime.UtcNow.Add(TrialPeriod).ToUnixSeconds()
                : 0
        };
        Subscriptions.Add(subscription);

        return () =>
            new PostResult<ChargebeeCreateSubscriptionResponse>(new ChargebeeCreateSubscriptionResponse
            {
                Customer = customer,
                Subscription = subscription
            });
    }

    public async Task<ApiGetResult<string, ChargebeeGetCustomerResponse>> GetCustomer(
        ChargebeeGetCustomerRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Fetching Customer via Chargebee: for: {Customer}",
            request.Id!);

        var customer = Customers.Find(cst => cst.Id == request.Id);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {request.Id} not found");
        }

        return () => new Result<ChargebeeGetCustomerResponse, Error>(new ChargebeeGetCustomerResponse
        {
            Customer = customer
        });
    }
    
    public async Task<ApiGetResult<string, ChargebeeListItemPricesResponse>> GetListItemPrices(
        ChargebeeListItemPricesRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Listing all Plans via Chargebee");
    
        return () => new Result<ChargebeeListItemPricesResponse, Error>(new ChargebeeListItemPricesResponse
        {
            List = Subscriptions
                .DistinctBy(subscription => subscription.SubscriptionItems[0].ItemPriceId)
                .Select(sub =>
                {
                    var firstItem = sub.SubscriptionItems.First();
                    return new ChargebeeItemPriceList
                    {
                        ItemPrice = new ChargebeeItemPrice
                        {
                            Id = firstItem.ItemPriceId,
                            CurrencyCode = sub.CurrencyCode,
                            Description = "A stubbed plan",
                            ExternalName = firstItem.ItemPriceId,
                            FreeQuantity = 0,
                            ItemFamilyId = ItemFamilyId,
                            ItemId = firstItem.ItemPriceId,
                            ItemType = "plan",
                            Period = 1,
                            PeriodUnit = "month",
                            Price = 30,
                            PricingModel = "flat_fee",
                            Status = Subscription.StatusEnum.Active.ToString(true),
                            TrialPeriod = 0,
                            TrialPeriodUnit = "month"
                        }
                    };
                }).ToList()
        });
    }

    public async Task<ApiGetResult<string, ChargebeeGetSubscriptionResponse>> GetSubscription(
        ChargebeeGetSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Fetching Subscription via Chargebee: for: {Subscription}",
            request.Id!);

        var subscription = Subscriptions.Find(s => s.Id == request.Id);
        if (subscription.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Subscription {request.Id} not found");
        }

        var customer = Customers.Find(c => c.Id == subscription.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {subscription.CustomerId} not found");
        }

        return () => new Result<ChargebeeGetSubscriptionResponse, Error>(new ChargebeeGetSubscriptionResponse
        {
            Customer = customer,
            Subscription = subscription
        });
    }

    public async Task<ApiGetResult<string, ChargebeeListAttachedItemsResponse>> ListAttachedItems(
        ChargebeeListAttachedItemsRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Listing all AttachedItems via Chargebee");

        return () => new Result<ChargebeeListAttachedItemsResponse, Error>(new ChargebeeListAttachedItemsResponse
        {
            List = new List<ChargebeeAttachedItemList>()
        });
    }

    public async Task<ApiGetResult<string, ChargebeeListFeaturesResponse>> ListFeatures(
        ChargebeeListFeaturesRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Listing all Features via Chargebee");

        return () => new Result<ChargebeeListFeaturesResponse, Error>(new ChargebeeListFeaturesResponse
        {
            List = new List<ChargebeeFeatureList>()
        });
    }
    
    public async Task<ApiGetResult<string, ChargebeeListInvoicesResponse>> ListInvoices(
        ChargebeeListInvoicesRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Listing all Invoices via Chargebee");
    
        return () => new Result<ChargebeeListInvoicesResponse, Error>(new ChargebeeListInvoicesResponse
        {
            List = new List<ChargebeeInvoiceList>()
        });
    }

    public async Task<ApiGetResult<string, ChargebeeListItemEntitlementsResponse>> ListItemEntitlements(
        ChargebeeListItemEntitlementsRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Listing all Item Entitlements via Chargebee");

        return () => new Result<ChargebeeListItemEntitlementsResponse, Error>(new ChargebeeListItemEntitlementsResponse
        {
            List = new List<ChargebeeItemEntitlementList>()
        });
    }

    public async Task<ApiPostResult<string, ChargebeeReactivateSubscriptionResponse>> ReactivateSubscription(
        ChargebeeReactivateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Reactivating Subscription via Chargebee: for: {Subscription}",
            request.Id!);

        var subscription = Subscriptions.Find(sub => sub.Id == request.Id);
        if (subscription.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Subscription {request.Id} not found");
        }

        var customer = Customers.Find(c => c.Id == subscription.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {subscription.CustomerId} not found");
        }

        subscription.CancelledAt = null;
        subscription.Deleted = null;
        subscription.Status = Subscription.StatusEnum.Active.ToString(true);

        return () =>
            new PostResult<ChargebeeReactivateSubscriptionResponse>(new ChargebeeReactivateSubscriptionResponse
            {
                Customer = customer,
                Subscription = subscription
            });
    }

    public async Task<ApiPostResult<string, ChargebeeRemoveScheduledCancellationSubscriptionResponse>>
        RemoveScheduledCancellationSubscription(ChargebeeRemoveScheduledCancellationSubscriptionRequest request,
            CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Removing cancellation for Subscription via Chargebee: for: {Subscription}",
            request.Id!);

        var subscription = Subscriptions.Find(sub => sub.Id == request.Id);
        if (subscription.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Subscription {request.Id} not found");
        }

        var customer = Customers.Find(cst => cst.Id == subscription.CustomerId);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {subscription.CustomerId} not found");
        }

        subscription.CancelledAt = null;
        subscription.Deleted = null;
        subscription.Status = Subscription.StatusEnum.Active.ToString(true);

        return () =>
            new PostResult<ChargebeeRemoveScheduledCancellationSubscriptionResponse>(
                new ChargebeeRemoveScheduledCancellationSubscriptionResponse
                {
                    Subscription = subscription,
                    Customer = customer
                });
    }

    public async Task<ApiPostResult<string, ChargebeeUpdateCustomerResponse>> UpdateCustomer(
        ChargebeeUpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Updating Customer via Chargebee: for: {Customer}",
            request.Id!);

        var customer = Customers.Find(cst => cst.Id == request.Id);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {request.Id} not found");
        }

        customer = request.Convert<ChargebeeUpdateCustomerRequest, ChargebeeCustomer>();

        return () =>
            new PostResult<ChargebeeUpdateCustomerResponse>(new ChargebeeUpdateCustomerResponse
            {
                Customer = customer
            });
    }

    public async Task<ApiPostResult<string, ChargebeeUpdateCustomerResponse>> UpdateCustomerBillingInfo(
        ChargebeeUpdateCustomerBillingInfoRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubChargebee: Updating Customer billing info via Chargebee: for: {Customer}",
            request.Id!);

        var customer = Customers.Find(cst => cst.Id == request.Id);
        if (customer.NotExists())
        {
            return () => Error.EntityNotFound($"StubChargebee: Customer {request.Id} not found");
        }

        customer = request.Convert<ChargebeeUpdateCustomerBillingInfoRequest, ChargebeeCustomer>();

        return () =>
            new PostResult<ChargebeeUpdateCustomerResponse>(new ChargebeeUpdateCustomerResponse
            {
                Customer = customer
            });
    }

    private static string GenerateRandomIdentifier()
    {
        return Guid.NewGuid().ToString("N").ToLowerInvariant();
    }
}