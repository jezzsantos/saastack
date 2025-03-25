using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Common;
using Common.Extensions;
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
    private readonly IAzureManagementApi _azureManagementApi;
    private readonly IEnvironmentVariables _environmentVariables;
    private readonly IRecorder _recorder;

    public AzureFunctionWorkersRuntime(IRecorder recorder, IEnvironmentVariables environmentVariables) : this(recorder,
        environmentVariables, new AzureManagementApi())
    {
    }

    internal AzureFunctionWorkersRuntime(IRecorder recorder, IEnvironmentVariables environmentVariables,
        IAzureManagementApi azureManagementApi)
    {
        _recorder = recorder;
        _environmentVariables = environmentVariables;
        _azureManagementApi = azureManagementApi;
    }

    public async Task CircuitBreakWorkerAsync(string workerName, CancellationToken cancellationToken)
    {
        var functionInfo = GetAzureFunctionInfo(_environmentVariables, workerName);
        var updated = await _azureManagementApi.DisableFunctionAsync(_recorder, functionInfo, cancellationToken);
        if (updated.IsFailure)
        {
            var ex = updated.Error.ToException<InvalidOperationException>();
            _recorder.TraceError(null, ex,
                "Failed to stop Azure function {FunctionName} and circuit break delivery of message. Error was: {Error}",
                functionInfo.FunctionName, ex);
        }
    }

    private static AzureFunctionInfo GetAzureFunctionInfo(IEnvironmentVariables environmentVariables,
        string functionName)
    {
        var functionAppName = environmentVariables.Get(WebsiteNameEnvironmentVariable).Value;
        var subscriptionId =
            environmentVariables.Get(WebsiteOwnerEnvironmentVariable)
                .Value; //e.g. adaeb017-1d53-477e-802a-25e959970081+aresourcegroupname-AustraliaEastwebspace
        subscriptionId =
            subscriptionId.Substring(0,
                subscriptionId.IndexOf("+",
                    StringComparison
                        .Ordinal));
        var resourceGroupName = environmentVariables.Get(WebsiteResourceGroupEnvironmentVariable).Value;
        return new AzureFunctionInfo
        {
            FunctionName = functionName,
            FunctionAppName = functionAppName,
            ResourceGroupName = resourceGroupName,
            SubscriptionId = subscriptionId
        };
    }

    public class AzureFunctionInfo
    {
        public string FunctionAppName { get; set; } = string.Empty;

        public string FunctionName { get; set; } = string.Empty;

        public string ResourceGroupName { get; set; } = string.Empty;

        public string SubscriptionId { get; set; } = string.Empty;
    }

    public interface IAzureManagementApi
    {
        Task<Result<Error>> DisableFunctionAsync(IRecorder recorder, AzureFunctionInfo info,
            CancellationToken cancellationToken);
    }

    private class AzureManagementApi : IAzureManagementApi
    {
        public async Task<Result<Error>> DisableFunctionAsync(IRecorder recorder, AzureFunctionInfo info,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = new ArmClient(new DefaultAzureCredential());
                var subscription =
                    client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{info.SubscriptionId}"));
                var resourceGroup =
                    (await subscription.GetResourceGroupAsync(info.ResourceGroupName, cancellationToken)).Value;
                var website = (await resourceGroup.GetWebSiteAsync(info.FunctionAppName, cancellationToken)).Value;
                var appSettings = (await website.GetApplicationSettingsAsync(cancellationToken)).Value;

                appSettings.Properties[$"AzureWebJobs.{info.FunctionName}.Disabled"] = "1";

                await website.UpdateApplicationSettingsAsync(appSettings, cancellationToken);
            }
            catch (Exception ex)
            {
                return ex.ToError(ErrorCode.Unexpected);
            }

            return Result.Ok;
        }
    }
}