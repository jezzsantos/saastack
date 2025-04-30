namespace OnPremRelay.WorkerHost.Messaging.Delivery;

/// <summary>
///     Custom EventArgs that include the routing key and the raw message.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the routing key of the received message.
    /// </summary>
    public string RoutingKey { get; }

    /// <summary>
    ///     Gets the raw message content.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReceivedEventArgs" /> class.
    /// </summary>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="message">The message content.</param>
    public MessageReceivedEventArgs(string routingKey, string message)
    {
        RoutingKey = routingKey;
        Message = message;
    }
}