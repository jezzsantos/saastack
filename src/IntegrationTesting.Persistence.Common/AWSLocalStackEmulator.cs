namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running LocalStack for integration testing.
/// </summary>
public static class AWSLocalStackEmulator
{
    private static readonly string ContainerName = "localstack-main";

    public static void Shutdown()
    {
        DockerImageEmulator.Shutdown();
    }

    public static void Start()
    {
        DockerImageEmulator.Start(ContainerName);
    }
}