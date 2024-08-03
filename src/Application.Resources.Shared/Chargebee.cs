using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Application.Resources.Shared;

public static class ChargebeeConstants
{
    public const string AuditSourceName = ProviderName;
    public const string ProviderName = "chargebee_billing";

    public static class MetadataProperties
    {
        public const string BillingAmount = "BillingAmount";
        public const string BillingPeriodUnit = "BillingPeriodUnit";
        public const string BillingPeriodValue = "BillingPeriodValue";
        public const string CanceledAt = "CanceledAt";
        public const string CurrencyCode = "CurrencyCode";
        public const string CustomerId = "CustomerId";
        public const string NextBillingAt = "NextBillingAt";
        public const string PaymentMethodStatus = "PaymentMethodStatus";
        public const string PaymentMethodType = "PaymentMethodType";
        public const string PlanId = "PlanId";
        public const string SubscriptionDeleted = "SubscriptionDeleted";
        public const string SubscriptionId = "SubscriptionId";
        public const string SubscriptionStatus = "SubscriptionStatus";
        public const string TrialEnd = "TrialEnd";
    }
}

public enum ChargebeeEventType
{
    Unknown = 0,
    [EnumMember(Value = "coupon_created")] CouponCreated,
    [EnumMember(Value = "coupon_updated")] CouponUpdated,
    [EnumMember(Value = "credit_note_created")]
    CreditNoteCreated,
    [EnumMember(Value = "credit_note_deleted")]
    CreditNoteDeleted,
    [EnumMember(Value = "credit_note_updated")]
    CreditNoteUpdated,
    [EnumMember(Value = "customer_changed")]
    CustomerChanged,
    [EnumMember(Value = "customer_created")]
    CustomerCreated,
    [EnumMember(Value = "customer_deleted")]
    CustomerDeleted,
    [EnumMember(Value = "invoice_deleted")]
    InvoiceDeleted,
    [EnumMember(Value = "invoice_generated")]
    InvoiceGenerated,
    [EnumMember(Value = "invoice_updated")]
    InvoiceUpdated,
    [EnumMember(Value = "item_updated")] ItemUpdated,
    [EnumMember(Value = "payment_failed")] PaymentFailed,
    [EnumMember(Value = "payment_initiated")]
    PaymentInitiated,
    [EnumMember(Value = "payment_refunded")]
    PaymentRefunded,
    [EnumMember(Value = "payment_source_added")]
    PaymentSourceAdded,
    [EnumMember(Value = "payment_source_deleted")]
    PaymentSourceDeleted,
    [EnumMember(Value = "payment_source_expired")]
    PaymentSourceExpired,
    [EnumMember(Value = "payment_source_expiring")]
    PaymentSourceExpiring,
    [EnumMember(Value = "payment_source_updated")]
    PaymentSourceUpdated,
    [EnumMember(Value = "payment_succeeded")]
    PaymentSucceeded,
    [EnumMember(Value = "plan_updated")] PlanUpdated,
    [EnumMember(Value = "subscription_activated")]
    SubscriptionActivated,
    [EnumMember(Value = "subscription_cancellation_reminder")]
    SubscriptionCancellationReminder,
    [EnumMember(Value = "subscription_cancellation_scheduled")]
    SubscriptionCancellationScheduled,
    [EnumMember(Value = "subscription_cancelled")]
    SubscriptionCancelled,
    [EnumMember(Value = "subscription_changed")]
    SubscriptionChanged,
    [EnumMember(Value = "subscription_changes_scheduled")]
    SubscriptionChangesScheduled,
    [EnumMember(Value = "subscription_created")]
    SubscriptionCreated,
    [EnumMember(Value = "subscription_deleted")]
    SubscriptionDeleted,
    [EnumMember(Value = "subscription_reactivated")]
    SubscriptionReactivated,
    [EnumMember(Value = "subscription_renewal_reminder")]
    SubscriptionRenewalReminder,
    [EnumMember(Value = "subscription_renewed")]
    SubscriptionRenewed,
    [EnumMember(Value = "subscription_scheduled_cancellation_removed")]
    SubscriptionScheduledCancellationRemoved,
    [EnumMember(Value = "subscription_scheduled_changes_removed")]
    SubscriptionScheduledChangesRemoved,
    [EnumMember(Value = "subscription_trial_extended")]
    SubscriptionTrialExtended
}

public class ChargebeeEventContent
{
    public ChargebeeEventCustomer? Customer { get; set; }

    public ChargebeeEventSubscription? Subscription { get; set; }
}

public class ChargebeeEventCustomer
{
    public string? Id { get; set; }

    [JsonPropertyName("payment_method")] public ChargebeePaymentMethod? PaymentMethod { get; set; }
}

public class ChargebeeEventSubscription
{
    [JsonPropertyName("billing_period")] public int? BillingPeriod { get; set; }

    [JsonPropertyName("billing_period_unit")]
    public string? BillingPeriodUnit { get; set; }

    [JsonPropertyName("cancelled_at")] public long? CancelledAt { get; set; }

    [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }

    [JsonPropertyName("customer_id")] public string? CustomerId { get; set; }

    [JsonPropertyName("deleted")] public bool? Deleted { get; set; }

    public string? Id { get; set; }

    [JsonPropertyName("next_billing_at")] public long? NextBillingAt { get; set; }

    public string? Status { get; set; }

    [JsonPropertyName("subscription_items")]
    public List<ChargebeeEventSubscriptionItem> SubscriptionItems { get; set; } = new();

    [JsonPropertyName("trial_end")] public long? TrialEnd { get; set; }

    [JsonPropertyName("trial_start")] public long? TrialStart { get; set; }
}

public class ChargebeeEventSubscriptionItem
{
    [JsonPropertyName("amount")] public decimal? Amount { get; set; } // total amount in cents

    [JsonPropertyName("item_price_id")] public string? ItemPriceId { get; set; } // plan id

    [JsonPropertyName("item_type")] public string? ItemType { get; set; } // values: plan, addon or charge

    [JsonPropertyName("quantity")] public int? Quantity { get; set; }

    [JsonPropertyName("unit_price")] public decimal? UnitPrice { get; set; } // price in cents
}

public class ChargebeePaymentMethod
{
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } //values: valid, expiring, expired, invalid, pending_verification

    [JsonPropertyName("type")]
    public string? Type { get; set; } // values: card, paypal_express_checkout, amazon_payments, direct_debit, etc, etc
}