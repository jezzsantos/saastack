using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can send email messages.
///     Delivery of the message can be confirmed by the specific 3rd party service, later on
/// </summary>
public interface IEmailDeliveryService
{
    /// <summary>
    ///     Sends an HTML email for delivery
    /// </summary>
    Task<Result<EmailDeliveryReceipt, Error>> SendHtmlAsync(ICallerContext caller, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Sends a templated email for delivery
    /// </summary>
    Task<Result<EmailDeliveryReceipt, Error>> SendTemplatedAsync(ICallerContext caller, string templateId,
        string? subject,
        Dictionary<string, string> substitutions,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken);
}

public class EmailDeliveryReceipt
{
    public string? ReceiptId { get; set; }
}