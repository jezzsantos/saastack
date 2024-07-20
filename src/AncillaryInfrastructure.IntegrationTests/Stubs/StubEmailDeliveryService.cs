using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubEmailDeliveryService : IEmailDeliveryService
{
    public List<string> AllSubjects { get; private set; } = new();

    public Optional<string> LastReceiptId { get; private set; } = Optional<string>.None;

    public Optional<string> LastSubject { get; private set; } = Optional<string>.None;

    public bool SendingSucceeds { get; set; } = true;

    public Task<Result<EmailDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        CancellationToken cancellationToken = default)
    {
        var receiptId = $"receipt_{Guid.NewGuid():N}";
        AllSubjects.Add(subject);
        LastSubject = Optional<string>.Some(subject);
        LastReceiptId = receiptId;

        return SendingSucceeds
            ? Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt
            {
                ReceiptId = receiptId
            })
            : Task.FromResult<Result<EmailDeliveryReceipt, Error>>(Error.Unexpected());
    }

    public void Reset()
    {
        AllSubjects = new List<string>();
        LastSubject = Optional<string>.None;
        LastReceiptId = Optional<string>.None;
    }
}