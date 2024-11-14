using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can send email messages.
///     Delivery of the message can be confirmed by the service later
/// </summary>
public interface IEmailDeliveryService
{
    /// <summary>
    ///     Sends the email for delivery
    /// </summary>
    Task<Result<EmailDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken = default);
}

public class EmailDeliveryReceipt
{
    public string? ReceiptId { get; set; }
}