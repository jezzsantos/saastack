using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines an email scheduling service, that will schedule messages for asynchronous and deferred delivery
/// </summary>
public interface IEmailSchedulingService
{
    Task<Result<Error>> ScheduleHtmlEmail(ICallerContext caller, HtmlEmail htmlEmail,
        CancellationToken cancellationToken);
}

/// <summary>
///     Defines the contents of an HTML email message
/// </summary>
public class HtmlEmail
{
    public required string Body { get; set; }

    public required string FromDisplayName { get; set; }

    public required string FromEmailAddress { get; set; }

    public required string Subject { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }

    public List<string>? Tags { get; set; }
}