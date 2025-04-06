using OnPremRelay.WorkerHost.Messaging.Delivery;

namespace OnPremRelay.WorkerHost.Messaging.Interfaces;

/// <summary>
///     Defines a contract for a message broker service.
/// </summary>
public interface IMessageBrokerService
{
    /// <summary>
    ///     Event raised when a message is received.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs> MessageReceived;

    /// <summary>
    ///     Starts the message broker service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Stops the message broker service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}