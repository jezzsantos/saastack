using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Microsoft;
using Infrastructure.Workers.Api;

namespace AzureFunctions.Api.WorkerHost;

/// <summary>
///     Provides the runtime environment for Azure Functions.
/// </summary>
public class AzureFunctionWorkersRuntime : IWorkersRuntime
{
    private readonly IServiceClient _serviceClient;

    public AzureFunctionWorkersRuntime(IServiceClientFactory serviceClientFactory)
    {
        _serviceClient = serviceClientFactory.CreateServiceClient("https://management.azure.com");
    }

    public async Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        var result = await _serviceClient.PostAsync(
            null, new StopAzureFunctionRequest
            {
                SubscriptionId = "asubscriptionid",
                ResourceGroupName = "aresourcegroupname",
                FunctionName = workerName
            }, null, cancellationToken);

        if (result.IsFailure)
        {
            throw result.Error.ToException();
        }
    }
}