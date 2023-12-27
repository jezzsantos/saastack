using IntegrationTesting.Persistence.Common;

namespace Infrastructure.Worker.Api.IntegrationTests.AWSLambdas;

public static class AWSAccountBase
{
    public static void CleanupAllTests()
    {
        AWSLocalStackEmulator.Shutdown();
    }

    public static void InitializeAllTests()
    {
        AWSLocalStackEmulator.Start();
    }
}