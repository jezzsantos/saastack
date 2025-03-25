using Common;
using Moq;
using Xunit;

namespace AzureFunctions.Api.WorkerHost.UnitTests;

[Trait("Category", "Unit")]
public class AzureFunctionWorkersRuntimeSpec
{
    private readonly Mock<AzureFunctionWorkersRuntime.IAzureManagementApi> _azureManagementApi;
    private readonly Mock<IEnvironmentVariables> _environmentVariables;
    private readonly Mock<IRecorder> _recorder;
    private readonly AzureFunctionWorkersRuntime _runtime;

    public AzureFunctionWorkersRuntimeSpec()
    {
        _recorder = new Mock<IRecorder>();
        _environmentVariables = new Mock<IEnvironmentVariables>();
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable))
            .Returns("anappname");
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable))
            .Returns("asubscriptionid+aresourcegroupname-aregionwebspace");
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable))
            .Returns("aresourcegroupname");
        _azureManagementApi = new Mock<AzureFunctionWorkersRuntime.IAzureManagementApi>();
        _azureManagementApi.Setup(sc => sc.DisableFunctionAsync(It.IsAny<IRecorder>(),
                It.IsAny<AzureFunctionWorkersRuntime.AzureFunctionInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _runtime = new AzureFunctionWorkersRuntime(_recorder.Object, _environmentVariables.Object,
            _azureManagementApi.Object);
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsync_ThenStopsFunction()
    {
        await _runtime.CircuitBreakWorkerAsync("aworkername", CancellationToken.None);

        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable));
        _azureManagementApi.Verify(ama => ama.DisableFunctionAsync(It.IsAny<IRecorder>(),
            It.Is<AzureFunctionWorkersRuntime.AzureFunctionInfo>(info =>
                info.SubscriptionId == "asubscriptionid"
                && info.ResourceGroupName == "aresourcegroupname"
                && info.FunctionAppName == "anappname"
                && info.FunctionName == "aworkername"
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsyncAndFails_ThenRecords()
    {
        _azureManagementApi.Setup(sc => sc.DisableFunctionAsync(It.IsAny<IRecorder>(),
                It.IsAny<AzureFunctionWorkersRuntime.AzureFunctionInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        await _runtime.CircuitBreakWorkerAsync("aworkername", CancellationToken.None);

        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable));
        _azureManagementApi.Verify(ama => ama.DisableFunctionAsync(It.IsAny<IRecorder>(),
            It.Is<AzureFunctionWorkersRuntime.AzureFunctionInfo>(info =>
                info.SubscriptionId == "asubscriptionid"
                && info.ResourceGroupName == "aresourcegroupname"
                && info.FunctionAppName == "anappname"
                && info.FunctionName == "aworkername"
            ), It.IsAny<CancellationToken>()));
        _recorder.Verify(r => r.TraceError(null, It.Is<Exception>(ex =>
            ex.Message == "Unexpected: amessage"), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()));
    }
}