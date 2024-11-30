using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines an email scheduling service, that will schedule messages for asynchronous and deferred delivery
/// </summary>
public interface IEmailSchedulingService
{
    Task<Result<Error>> ScheduleHtmlEmail(ICallerContext caller, HtmlEmail email,
        CancellationToken cancellationToken);

    Task<Result<Error>> ScheduleTemplatedEmail(ICallerContext caller, TemplatedEmail email,
        CancellationToken cancellationToken);
}

/// <summary>
///     Defines an HTML email message
/// </summary>
public class HtmlEmail
{
    public required string Body { get; set; }

    public required string FromDisplayName { get; set; }

    public required string FromEmailAddress { get; set; }

    public required string Subject { get; set; }

    public List<string>? Tags { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }
}

/// <summary>
///     Defines an email template message
/// </summary>
public class TemplatedEmail
{
    public required string FromDisplayName { get; set; }

    public required string FromEmailAddress { get; set; }

    public string? Subject { get; set; }

    public Dictionary<string, string> Substitutions { get; set; } = new();

    public List<string>? Tags { get; set; }

    public required string TemplateId { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }
}