using Application.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace AncillaryInfrastructure.IntegrationTests.Stubs;

public sealed class StubEmailDeliveryService : IEmailDeliveryService
{
    public List<string> AllSubjects { get; private set; } = new();

    public Optional<string> LastSubject { get; private set; } = Optional<string>.None;

    public Task<Result<Error>> DeliverAsync(ICallerContext context, string subject, string htmlBody,
        string toEmailAddress, string? toDisplayName,
        string fromEmailAddress, string? fromDisplayName, CancellationToken cancellationToken = default)
    {
        AllSubjects.Add(subject);
        LastSubject = Optional<string>.Some(subject);

        return Task.FromResult(Result.Ok);
    }

    public void Reset()
    {
        AllSubjects = new List<string>();
        LastSubject = Optional<string>.None;
    }
}