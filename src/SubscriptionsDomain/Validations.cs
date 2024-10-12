using Domain.Interfaces.Validations;

namespace SubscriptionsDomain;

public static class Validations
{
    public static class Subscription
    {
        public static readonly DateTime MinInvoiceDate = new(2024, 06, 01, 0, 0, 0, DateTimeKind.Utc);
        public static readonly TimeSpan DefaultInvoicePeriod = MinInvoiceDate.AddMonths(3).Subtract(MinInvoiceDate);
        public static readonly DateTime MaxInvoiceDate =
            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 01, 0, 0, 0, DateTimeKind.Utc).AddMonths(3);

        public static readonly Validation PlanId = CommonValidations.DescriptiveName(1, 50);
        public static readonly Validation ProviderName = CommonValidations.DescriptiveName(1, 50);
    }
}