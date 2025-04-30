namespace OnPremRelay.WorkerHost.Messaging.Interfaces;

public interface IMessageDeliveryHandler
{
    string WorkerName { get; }
    Task CompleteMessageAsync(object messageContext, CancellationToken cancellationToken);
    Task AbandonMessageAsync(object messageContext, CancellationToken cancellationToken);
    Task CheckCircuitAsync(string workerName, int currentFailureCount, CancellationToken cancellationToken);
}
