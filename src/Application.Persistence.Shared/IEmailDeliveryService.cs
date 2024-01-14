using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can deliver email events
/// </summary>
public interface IEmailDeliveryService
{
    /// <summary>
    ///     Delivers the email
    /// </summary>
    Task<Result<EmailDeliveryReceipt, Error>> DeliverAsync(ICallerContext context, string subject, string htmlBody,
        string toEmailAddress,
        string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken = default);
}

public class EmailDeliveryReceipt
{
    public string? TransactionId { get; set; }
}