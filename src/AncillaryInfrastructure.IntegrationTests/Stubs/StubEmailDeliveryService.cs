using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubEmailDeliveryService : IEmailDeliveryService
{
    public List<string> AllSubjects { get; private set; } = new();

    public bool DeliverySucceeds { get; set; } = true;

    public Optional<string> LastSubject { get; private set; } = Optional<string>.None;

    public Task<Result<EmailDeliveryReceipt, Error>> DeliverAsync(ICallerContext context, string subject,
        string htmlBody,
        string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken = default)
    {
        AllSubjects.Add(subject);
        LastSubject = Optional<string>.Some(subject);

        return DeliverySucceeds
            ? Task.FromResult<Result<EmailDeliveryReceipt, Error>>(new EmailDeliveryReceipt
            {
                TransactionId = "atransactionid"
            })
            : Task.FromResult<Result<EmailDeliveryReceipt, Error>>(Error.Unexpected());
    }

    public void Reset()
    {
        AllSubjects = new List<string>();
        LastSubject = Optional<string>.None;
    }
}