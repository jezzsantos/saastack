using Common;
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
    internal const string WebsiteNameEnvironmentVariable = "WEBSITE_SITE_NAME";
    internal const string WebsiteOwnerEnvironmentVariable = "WEBSITE_OWNER_NAME";
    internal const string WebsiteResourceGroupEnvironmentVariable = "WEBSITE_RESOURCE_GROUP";
    private readonly IEnvironmentVariables _environmentVariables;
    private readonly IServiceClient _serviceClient;

    public AzureFunctionWorkersRuntime(IServiceClientFactory serviceClientFactory,
        IEnvironmentVariables environmentVariables)
    {
        _environmentVariables = environmentVariables;
        _serviceClient = serviceClientFactory.CreateServiceClient("https://management.azure.com");
    }

    public async Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        var functionAppName = _environmentVariables.Get(WebsiteNameEnvironmentVariable).Value;
        var subscriptionId =
            _environmentVariables.Get(WebsiteOwnerEnvironmentVariable)
                .Value; //e.g. adaeb017-1d53-477e-802a-25e959970081+aresourcegroupname-AustraliaEastwebspace
        subscriptionId =
            subscriptionId.Substring(0,
                subscriptionId.IndexOf("+",
                    StringComparison
                        .Ordinal));
        var resourceGroupName = _environmentVariables.Get(WebsiteResourceGroupEnvironmentVariable).Value;

        // TODO: some way to authorize this call, with a managed identity.
        var result = await _serviceClient.PutAsync(
            null, new StopAzureFunctionRequest
            {
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                FunctionName = workerName,
                FunctionAppName = functionAppName
            }, null, cancellationToken);

        if (result.IsFailure)
        {
            throw result.Error.ToException();
        }
    }
}