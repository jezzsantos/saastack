using System.Diagnostics;
using Common.Extensions;
using OperatingSystem = System.OperatingSystem;

namespace IntegrationTesting.Persistence.Common;

public static class AWSLocalStackEmulator
{
    private static readonly string CommandLine = "docker";
    private static readonly string ContainerName = "localstack-main";
    private static readonly string CommandLineArguments = $"ps -f \"name={ContainerName}\"";

    public static void Shutdown()
    {
        ShutdownEmulator();
    }

    public static void Start()
    {
        ShutdownEmulator();
        StartEmulator();
    }

    private static void StartEmulator()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException(Resources.UnSupportedPlatform);
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = CommandLine,
            Arguments = CommandLineArguments,
            RedirectStandardOutput = true
        });
        if (process.NotExists())
        {
            throw new InvalidOperationException(
                Resources.LocalStackEmulator_StartEmulator_FailedStartup.Format(CommandLine, CommandLineArguments));
        }

        if (process.HasExited)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(Resources.LocalStackEmulator_StartEmulator_Exited.Format(error));
        }

        var stdOut = process.StandardOutput.ReadToEnd();
        if (!stdOut.Contains(ContainerName))
        {
            throw new InvalidOperationException(Resources.LocalStackEmulator_StartEmulator_LocalStackNotRunning);
        }
    }

    private static void ShutdownEmulator()
    {
    }
}