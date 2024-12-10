using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubSmsDeliveryService : ISmsDeliveryService
{
    public List<string> AllPhoneNumbers { get; private set; } = new();

    public Optional<string> LastPhoneNumber { get; private set; } = Optional<string>.None;

    public Optional<string> LastReceiptId { get; private set; } = Optional<string>.None;

    public bool SendingSucceeds { get; set; } = true;

    public Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string body,
        string toPhoneNumber, IReadOnlyList<string>? tags, CancellationToken cancellationToken)
    {
        var receiptId = $"receipt_{Guid.NewGuid():N}";
        AllPhoneNumbers.Add(toPhoneNumber);
        LastPhoneNumber = Optional<string>.Some(toPhoneNumber);
        LastReceiptId = receiptId;

        return SendingSucceeds
            ? Task.FromResult<Result<SmsDeliveryReceipt, Error>>(new SmsDeliveryReceipt
            {
                ReceiptId = receiptId
            })
            : Task.FromResult<Result<SmsDeliveryReceipt, Error>>(Error.Unexpected());
    }

    public void Reset()
    {
        AllPhoneNumbers = new List<string>();
        LastPhoneNumber = Optional<string>.None;
        LastReceiptId = Optional<string>.None;
    }
}