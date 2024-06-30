using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Shared.Subscriptions;

namespace Application.Services.Shared;

/// <summary>
///     Defines the gateway to the billing management service, for performing transactional operations on the subscription
/// </summary>
public interface IBillingGatewayService
{
    /// <summary>
    ///     Cancels the subscription for the current buyer
    /// </summary>
    Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken);

    /// <summary>
    ///     Changes the plan for the subscription
    /// </summary>
    Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all the pricing plans for the current provider
    /// </summary>
    Task<Result<PricingPlans, Error>> ListAllPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Lists all invoices for the subscription, given the specified date range, and options
    /// </summary>
    Task<Result<List<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller, BillingProvider provider,
        DateTime fromUtc, DateTime toUtc, SearchOptions searchOptions, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a new subscription the specified <see cref="buyer" />
    /// </summary>
    Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller, SubscriptionBuyer buyer,
        SubscribeOptions options, CancellationToken cancellationToken);

    /// <summary>
    ///     Transfers the subscription from the current buyer to another buyer
    /// </summary>
    Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken);
}

/// <summary>
///     Options for changing a subscription plan
/// </summary>
public class ChangePlanOptions
{
    public required string OwningEntityId { get; set; }

    public required string PlanId { get; set; }
}

/// <summary>
///     Options for canceling a subscription
/// </summary>
public class CancelSubscriptionOptions
{
    public static readonly CancelSubscriptionOptions EndOfTerm = new()
    {
        CancelWhen = CancelSubscriptionSchedule.EndOfTerm,
        FutureTime = null
    };
    public static readonly CancelSubscriptionOptions Immediately = new()
    {
        CancelWhen = CancelSubscriptionSchedule.Immediately,
        FutureTime = null
    };

    public CancelSubscriptionSchedule CancelWhen { get; set; }

    public DateTime? FutureTime { get; set; }

    public static CancelSubscriptionOptions AtScheduledTime(DateTime time)
    {
        return new CancelSubscriptionOptions
        {
            CancelWhen = CancelSubscriptionSchedule.Scheduled,
            FutureTime = time
        };
    }
}

/// <summary>
///     Defines the schedule for canceling a subscription
/// </summary>
public enum CancelSubscriptionSchedule
{
    Immediately = 0,
    EndOfTerm = 1,
    Scheduled = 2
}

/// <summary>
///     Options for transferring a subscription
/// </summary>
public class TransferSubscriptionOptions
{
    public string? PlanId { get; set; }

    public required SubscriptionBuyer TransfereeBuyer { get; set; }
}

/// <summary>
///     Defines the options for creating a new subscription
/// </summary>
public class SubscribeOptions
{
    public static readonly SubscribeOptions Immediately = new()
    {
        StartWhen = StartSubscriptionSchedule.Immediately,
        FutureTime = null,
        PlanId = null
    };

    public DateTime? FutureTime { get; set; }

    public string? PlanId { get; set; }

    public StartSubscriptionSchedule StartWhen { get; set; }

    public static SubscribeOptions AtScheduledTime(DateTime time)
    {
        return new SubscribeOptions
        {
            StartWhen = StartSubscriptionSchedule.Scheduled,
            FutureTime = time,
            PlanId = null
        };
    }
}

/// <summary>
///     Defines the schedule for starting a subscription
/// </summary>
public enum StartSubscriptionSchedule
{
    Immediately = 0,
    Scheduled = 2
}

/// <summary>
///     Defines the buyer of a subscription
/// </summary>
public class SubscriptionBuyer
{
    public required ProfileAddress Address { get; set; }

    public required string CompanyReference { get; set; }

    public required string EmailAddress { get; set; }

    public required string Id { get; set; }

    public required PersonName Name { get; set; }

    public string? PhoneNumber { get; set; }
}