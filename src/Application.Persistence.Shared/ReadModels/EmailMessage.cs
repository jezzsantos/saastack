using Application.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.Emails)]
public class EmailMessage : QueuedMessage
{
    public QueuedEmailHtmlMessage? Html { get; set; }
}

public class QueuedEmailHtmlMessage
{
    public string? FromDisplayName { get; set; }

    public string? FromEmailAddress { get; set; }

    public string? HtmlBody { get; set; }

    public string? Subject { get; set; }

    public List<string>? Tags { get; set; }

    public string? ToDisplayName { get; set; }

    public string? ToEmailAddress { get; set; }
}