using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Subscription : IIdentifiableResource
{
    public required string BuyerId { get; set; }

    public required string OwningEntityId { get; set; }

    public string? ProviderName { get; set; }

    public Dictionary<string, string> ProviderState { get; set; } = new();

    public required string Id { get; set; }
}

public class SubscriptionWithPlan : Subscription
{
    public bool CanBeCanceled { get; set; }

    public bool CanBeUnsubscribed { get; set; }

    public DateTime? CanceledDateUtc { get; set; }

    public required InvoiceSummary Invoice { get; set; }

    public required SubscriptionPaymentMethod PaymentMethod { get; set; }

    public required PlanPeriod Period { get; set; }

    public required SubscriptionPlan Plan { get; set; }

    public SubscriptionStatus Status { get; set; }

    public string? SubscriptionReference { get; set; }

    public required string BuyerReference { get; set; }
}

public class SubscriptionPlan
{
    public string? Id { get; set; }

    public bool IsTrial { get; set; }

    public SubscriptionTier Tier { get; set; }

    public DateTime? TrialEndDateUtc { get; set; }
}

public class PlanPeriod
{
    public int Frequency { get; set; }

    public PeriodFrequencyUnit Unit { get; set; }
}

public enum PeriodFrequencyUnit
{
    Eternity = 0,
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4
}

public enum SubscriptionStatus
{
    Unsubscribed = 0,
    Activated = 1,
    Canceled = 2,
    Canceling = 3
}

public enum SubscriptionTier
{
    Unsubscribed = 0,
    Standard = 1,
    Professional = 2,
    Enterprise = 3
}

public class Invoice : IIdentifiableResource
{
    public decimal? Amount { get; set; } // In the denomination of the Currency

    public required string Currency { get; set; } // ISO4217

    public bool IncludesTax { get; set; }

    public DateTime? InvoicedOnUtc { get; set; }

    public List<InvoiceLineItem> LineItems { get; set; } = new();

    public List<InvoiceNote> Notes { get; set; } = new();

    public InvoiceItemPayment? Payment { get; set; }

    public DateTime? PeriodEndUtc { get; set; }

    public DateTime? PeriodStartUtc { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal? TaxAmount { get; set; } // In the denomination of the Currency

    public required string Id { get; set; }
}

public class InvoiceSummary
{
    public decimal Amount { get; set; } // In the denomination of the Currency

    public required string Currency { get; set; } // ISO4217

    public DateTime? NextUtc { get; set; }
}

public class InvoiceLineItem
{
    public decimal? Amount { get; set; } // In the denomination of the Currency

    public required string Currency { get; set; } // ISO4217

    public required string Description { get; set; }

    public bool IsTaxed { get; set; }

    public required string Reference { get; set; }

    public decimal? TaxAmount { get; set; } // In the denomination of the Currency
}

public class InvoiceItemPayment
{
    public decimal? Amount { get; set; } // In the denomination of the Currency

    public required string Currency { get; set; } // ISO4217

    public DateTime? PaidOnUtc { get; set; }

    public required string Reference { get; set; }
}

public class InvoiceNote
{
    public required string Description { get; set; }
}

public enum InvoiceStatus
{
    Unpaid,
    Paid
}

public class SubscriptionPaymentMethod
{
    public static readonly SubscriptionPaymentMethod None = new()
    {
        Status = PaymentMethodStatus.Invalid,
        Type = PaymentMethodType.None,
        ExpiresOn = null
    };

    public DateOnly? ExpiresOn { get; set; }

    public PaymentMethodStatus Status { get; set; }

    public PaymentMethodType Type { get; set; }
}

public enum PaymentMethodType
{
    None = 0,
    Card = 1,
    Other = 2
}

public enum PaymentMethodStatus
{
    Invalid = 0,
    Valid = 1
}

public class PricingPlans
{
    public List<PricingPlan> Annually { get; set; } = new();

    public List<PricingPlan> Daily { get; set; } = new();

    public List<PricingPlan> Eternally { get; set; } = new();

    public List<PricingPlan> Monthly { get; set; } = new();

    public List<PricingPlan> Weekly { get; set; } = new();
}

public class PricingPlan : IIdentifiableResource
{
    public decimal Cost { get; set; } // In the denomination of the Currency

    public required string Currency { get; set; } // ISO4217

    public string? Description { get; set; }

    public string? DisplayName { get; set; }

    public List<PricingFeatureSection> FeatureSection { get; set; } = new();

    public bool IsRecommended { get; set; }

    public string? Notes { get; set; }

    public required PlanPeriod Period { get; set; }

    public decimal SetupCost { get; set; } // In the denomination of the Currency

    public SubscriptionTrialPeriod? Trial { get; set; }

    public required string Id { get; set; }
}

public class SubscriptionTrialPeriod
{
    public int Frequency { get; set; }

    public bool HasTrial { get; set; }

    public PeriodFrequencyUnit Unit { get; set; }
}

public class PricingFeatureSection
{
    public string? Description { get; set; }

    public List<PricingFeatureItem> Features { get; set; } = new();
}

public class PricingFeatureItem
{
    public string? Description { get; set; }

    public bool IsIncluded { get; set; }
}

public class SubscriptionToMigrate : Subscription
{
#pragma warning disable SAASAPP014
    public Dictionary<string, string> Buyer { get; set; } = new();
#pragma warning restore SAASAPP014
}