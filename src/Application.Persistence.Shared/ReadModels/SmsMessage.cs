using Application.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.Smses)]
public class SmsMessage : QueuedMessage
{
    public QueuedSmsMessage? Message { get; set; }
}

public class QueuedSmsMessage
{
    public string? Body { get; set; }

    public List<string>? Tags { get; set; }

    public string? ToPhoneNumber { get; set; }
}