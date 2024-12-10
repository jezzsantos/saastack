using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubEmailDeliveryService : IEmailDeliveryService
{
    public List<string> AllSubjects { get; private set; } = new();

    public List<string> AllTemplates { get; private set; } = new();

    public Optional<string> LastReceiptId { get; private set; } = Optional<string>.None;

    public Optional<string> LastSubject { get; private set; } = Optional<string>.None;

    public Optional<string> LastTemplate { get; private set; } = Optional<string>.None;

    public bool SendingSucceeds { get; set; } = true;

    public Task<Result<EmailDeliveryReceipt, Error>> SendHtmlAsync(ICallerContext caller, string subject,
        string htmlBody, string toEmailAddress, string? toDisplayName, string fromEmailAddress, string? fromDisplayName,
        IReadOnlyList<string>? tags, CancellationToken cancellationToken)
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

    public Task<Result<EmailDeliveryReceipt, Error>> SendTemplatedAsync(ICallerContext caller, string templateId,
        string? subject, Dictionary<string, string> substitutions, string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var receiptId = $"receipt_{Guid.NewGuid():N}";
        AllTemplates.Add(templateId);
        LastTemplate = Optional<string>.Some(templateId);
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
        AllTemplates = new List<string>();
        LastTemplate = Optional<string>.None;
    }
}