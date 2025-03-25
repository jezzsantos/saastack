using Xunit;

namespace AWSLambdas.Api.WorkerHost.UnitTests;

[Trait("Category", "Unit")]
public class AWSLambdaWorkerRuntimeSpec
{
    private readonly AWSLambdaWorkerRuntime _runtime;

    public AWSLambdaWorkerRuntimeSpec()
    {
        _runtime = new AWSLambdaWorkerRuntime();
    }

    [Fact]
    public async Task WhenCircuitBreakWorkerAsync_ThenReturns()
    {
        await _runtime.CircuitBreakWorkerAsync("aworkername", CancellationToken.None);
    }
}