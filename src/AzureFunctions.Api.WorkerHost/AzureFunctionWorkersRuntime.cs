using Infrastructure.Workers.Api;

namespace AzureFunctions.Api.WorkerHost;

/// <summary>
///     Provides the runtime environment for Azure Functions.
/// </summary>
public class AzureFunctionWorkersRuntime : IWorkersRuntime
{
    public async Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        //TODO: disable the Azure function

        // var stopFunctionRequest = new DurableHttpRequest(
        //     HttpMethod.Post, 
        //     new Uri($"https://management.azure.com{resourceId}/stop?api-version=2016-08-01"),
        //     tokenSource: new ManagedIdentityTokenSource("https://management.core.windows.net"));
        //
        throw new NotImplementedException();
    }
}