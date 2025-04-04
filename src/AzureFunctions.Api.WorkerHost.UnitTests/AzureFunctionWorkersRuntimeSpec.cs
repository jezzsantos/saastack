using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Microsoft;
using Moq;
using Xunit;

namespace AzureFunctions.Api.WorkerHost.UnitTests;

[Trait("Category", "Unit")]
public class AzureFunctionWorkersRuntimeSpec
{
    private readonly Mock<IEnvironmentVariables> _environmentVariables;
    private readonly AzureFunctionWorkersRuntime _runtime;
    private readonly Mock<IServiceClient> _serviceClient;

    public AzureFunctionWorkersRuntimeSpec()
    {
        _serviceClient = new Mock<IServiceClient>();
        var serviceClientFactory = new Mock<IServiceClientFactory>();
        serviceClientFactory.Setup(sc => sc.CreateServiceClient(It.IsAny<string>()))
            .Returns(_serviceClient.Object);
        _environmentVariables = new Mock<IEnvironmentVariables>();
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable))
            .Returns("anappname");
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable))
            .Returns("asubscriptionid+aresourcegroupname-aregionwebspace");
        _environmentVariables.Setup(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable))
            .Returns("aresourcegroupname");
        _runtime = new AzureFunctionWorkersRuntime(serviceClientFactory.Object, _environmentVariables.Object);
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsync_ThenStopsFunction()
    {
        await _runtime.CircuitBreakWorkerAsync("aworkername", CancellationToken.None);

        _serviceClient.Verify(sc => sc.PutAsync(null, It.Is<StopAzureFunctionRequest>(req =>
            req.FunctionName == "aworkername"
            && req.FunctionAppName == "anappname"
            && req.SubscriptionId == "asubscriptionid"
            && req.ResourceGroupName == "aresourcegroupname"
        ), null, It.IsAny<CancellationToken>()));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable));
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsyncAndFails_ThenThrows()
    {
        _serviceClient.Setup(sc => sc.PutAsync(It.IsAny<ICallerContext>(), It.IsAny<StopAzureFunctionRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseProblem { Status = 500, Title = "atitle", Detail = "adetail" });

        await _runtime.Invoking(x => x.CircuitBreakWorkerAsync("aworkername", CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("500: atitle, adetail");

        _serviceClient.Verify(sc => sc.PutAsync(null, It.Is<StopAzureFunctionRequest>(req =>
            req.FunctionName == "aworkername"
            && req.FunctionAppName == "anappname"
            && req.SubscriptionId == "asubscriptionid"
            && req.ResourceGroupName == "aresourcegroupname"
        ), null, It.IsAny<CancellationToken>()));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteNameEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteOwnerEnvironmentVariable));
        _environmentVariables.Verify(ev => ev.Get(AzureFunctionWorkersRuntime.WebsiteResourceGroupEnvironmentVariable));
    }
}