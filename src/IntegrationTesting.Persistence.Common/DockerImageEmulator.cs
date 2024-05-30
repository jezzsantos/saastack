using System.Diagnostics;
using Common.Extensions;
using OperatingSystem = System.OperatingSystem;

namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running Docker images for integration testing.
/// </summary>
public static class DockerImageEmulator
{
    private static readonly string CommandLine = "docker";
    private static readonly string CommandLineArguments = "ps -f \"name={0}\"";

    public static void Shutdown()
    {
        ShutdownEmulator();
    }

    public static void Start(string containerName, string? customErrorMessage = null)
    {
        ShutdownEmulator();
        StartEmulator(containerName, customErrorMessage);
    }

    private static void StartEmulator(string containerName, string? customErrorMessage = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException(Resources.UnSupportedPlatform);
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = CommandLine,
            Arguments = CommandLineArguments.Format(containerName),
            RedirectStandardOutput = true
        });
        if (process.NotExists())
        {
            throw new InvalidOperationException(
                Resources.DockerImageEmulator_StartEmulator_FailedStartup.Format(CommandLine,
                    CommandLineArguments.Format(containerName)));
        }

        if (process.HasExited)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(Resources.DockerImageEmulator_StartEmulator_Exited.Format(error));
        }

        var stdOut = process.StandardOutput.ReadToEnd();
        if (!stdOut.Contains(containerName))
        {
            throw new InvalidOperationException(customErrorMessage.HasValue()
                ? customErrorMessage.Format(containerName)
                : Resources.DockerImageEmulator_StartEmulator_ContainerNotRunning.Format(containerName));
        }
    }

    private static void ShutdownEmulator()
    {
    }
}