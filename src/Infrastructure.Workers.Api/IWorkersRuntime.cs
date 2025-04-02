namespace Infrastructure.Workers.Api;

/// <summary>
///     Defines a runtime for workers
/// </summary>
public interface IWorkersRuntime
{
    /// <summary>
    ///     Opens the circuit breaker, and disables the worker
    /// </summary>
    Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken);
}