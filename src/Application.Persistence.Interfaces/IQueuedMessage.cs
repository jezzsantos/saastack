namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a message that is queued on a queue or a bus.
///     Note: Must be fully serializable to be used in a queue.
/// </summary>
public interface IQueuedMessage
{
    /// <summary>
    ///     Returns the ID of the caller
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
    ///     Returns the region of the host that originated the message
    /// </summary>
    string? OriginHostRegion { get; set; } //Nullable for backwards compatibility

    /// <summary>
    ///     Returns the ID of the tenant
    /// </summary>
    string? TenantId { get; set; }
}