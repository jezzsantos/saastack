using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Microsoft;
using Moq;
using Xunit;

namespace AzureFunctions.Api.WorkerHost.UnitTests;

[Trait("Category", "Unit")]
public class AzureFunctionWorkersRuntimeSpec
{
    private readonly AzureFunctionWorkersRuntime _runtime;
    private readonly Mock<IServiceClient> _serviceClient;

    public AzureFunctionWorkersRuntimeSpec()
    {
        _serviceClient = new Mock<IServiceClient>();
        _runtime = new AzureFunctionWorkersRuntime(_serviceClient.Object);
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsync_ThenStopsFunction()
    {
        await _runtime.CircuitBreakWorkerAsync("aworkername", CancellationToken.None);

        _serviceClient.Verify(sc => sc.PostAsync(null, It.Is<StopAzureFunctionRequest>(req =>
            req.ResourceId == "aresourceid"), null, It.IsAny<CancellationToken>()));
    }
}