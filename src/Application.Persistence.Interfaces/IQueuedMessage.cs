namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a message that is queued on a queue
/// </summary>
public interface IQueuedMessage
{
    /// <summary>
    ///     Returns hte ID of the caller
    /// </summary>
    string CallerId { get; set; }

    /// <summary>
    ///     Returns the ID of the call
    /// </summary>
    string CallId { get; set; }

    /// <summary>
    ///     Returns the unique ID of the message
    /// </summary>
    string? MessageId { get; set; }

    /// <summary>
    ///     Returns hte ID of the tenant
    /// </summary>
    string? TenantId { get; set; }
}