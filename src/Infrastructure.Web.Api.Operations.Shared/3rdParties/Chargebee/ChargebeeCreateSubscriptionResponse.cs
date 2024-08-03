using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeCreateSubscriptionResponse : IWebResponse
{
    public ChargebeeCustomer? Customer { get; set; }

    public ChargebeeSubscription? Subscription { get; set; }
}

public class ChargebeeSubscription
{
    public int BillingPeriod { get; set; }

    public string? BillingPeriodUnit { get; set; }

    public long? CancelledAt { get; set; }

    public string? CurrencyCode { get; set; }

    public string? CustomerId { get; set; }

    public bool? Deleted { get; set; }

    public string? Id { get; set; }

    public long? NextBillingAt { get; set; }

    public string? Status { get; set; }

    public List<ChargebeeSubscriptionItem> SubscriptionItems { get; set; } = new();

    public long? TrialEnd { get; set; }
}

public class ChargebeeCustomer
{
    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? Id { get; set; }

    public string? LastName { get; set; }

    public string? Phone { get; set; }
}