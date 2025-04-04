using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost;

/// <summary>
///     Provides the runtime environment for AWS Lambdas.
/// </summary>
public class AWSLambdaWorkerRuntime : IWorkersRuntime
{
    public Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        //TODO: how to disable the Lambda?
        return Task.CompletedTask;
    }
}