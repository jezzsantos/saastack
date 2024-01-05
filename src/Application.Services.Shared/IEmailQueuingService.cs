using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines an asynchronous email delivery service, that will queue messages for delivery
/// </summary>
public interface IEmailQueuingService
{
    Task<Result<Error>> SendHtmlEmail(ICallerContext caller, HtmlEmail htmlEmail, CancellationToken cancellationToken);
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
}