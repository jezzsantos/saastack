using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines an SMS scheduling service, that will schedule messages for asynchronous and deferred delivery
/// </summary>
public interface ISmsSchedulingService
{
    Task<Result<Error>> ScheduleSms(ICallerContext caller, SmsText smsText,
        CancellationToken cancellationToken);
}

/// <summary>
///     Defines the contents of an SMS text message
/// </summary>
public class SmsText
{
    public required string Body { get; set; }

    public List<string>? Tags { get; set; }

    public required string To { get; set; }
}