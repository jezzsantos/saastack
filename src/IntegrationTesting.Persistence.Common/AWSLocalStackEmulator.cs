using DotNet.Testcontainers.Containers;
using Testcontainers.LocalStack;

namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running Azurite Storage Emulator for integration testing.
/// </summary>
public class AWSLocalStackEmulator
{
    private const string DockerImageName = "localstack/localstack:stable";

    private readonly LocalStackContainer _localStack = new LocalStackBuilder()
        .WithImage(DockerImageName)
        .Build();

    public string GetConnectionString()
    {
        if (!IsRunning())
        {
            throw new InvalidOperationException(
                "LocalStack emulator must be started before getting the connection string.");
        }

        return _localStack.GetConnectionString();
    }

    private bool IsRunning()
    {
        return _localStack.State == TestcontainersStates.Running;
    }

    public async Task StartAsync()
    {
        await _localStack.StartAsync();
    }

    public async Task StopAsync()
    {
        await _localStack.DisposeAsync();
    }
}