using Infrastructure.Workers.Api;

namespace AWSLambdas.Api.WorkerHost;

/// <summary>
///     Provides the runtime environment for AWS Lambdas.
/// </summary>
public class AWSLambdaWorkerRuntime : IWorkersRuntime
{
    public async Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        //TODO: disable the Lambda
        throw new NotImplementedException();
    }
}