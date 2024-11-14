using Application.Interfaces;
using Common;

namespace Application.Persistence.Shared;

/// <summary>
///     Defines a service to which we can send SMS messages.
///     Delivery of the message can be confirmed by the service later
/// </summary>
public interface ISmsDeliveryService
{
    /// <summary>
    ///     Sends the SMS for delivery
    /// </summary>
    Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string body,
        string toPhoneNumber, CancellationToken cancellationToken = default);
}

public class SmsDeliveryReceipt
{
    public string? ReceiptId { get; set; }
}