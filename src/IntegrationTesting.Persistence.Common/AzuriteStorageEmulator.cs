using System.Diagnostics;
using Common;
using Common.Extensions;
using UnitTesting.Common;
using OperatingSystem = System.OperatingSystem;

namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running Azurite Storage Emulator for integration testing.
/// </summary>
public static class AzuriteStorageEmulator
{
    private const string AzuriteJsName = @"azurite";
    private static readonly string ToolsFolder =
        Path.Combine(Solution.NavigateUpToSolutionDirectoryPath(), "../tools/azurite");
    private static readonly string AzuriteLocalStorageFolder =
        Path.GetFullPath(Path.Combine(ToolsFolder, "azurite"));

    private static readonly string AzuriteStartupArgs =
        $@"--silent --location {AzuriteLocalStorageFolder} --debug {AzuriteLocalStorageFolder}\debug.log";
    private static readonly string CommandLine = NodeJsProcessName;
    private static readonly string CommandLineArguments =
        $"{Path.GetFullPath(Path.Combine(ToolsFolder, "node_modules/azurite/dist/src"))}/{AzuriteJsName}.js {AzuriteStartupArgs}";

    private static string NodeJsProcessName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "node.exe";
            }

            return OperatingSystem.IsLinux()
                ? "node"
                : throw new InvalidOperationException(Resources.UnSupportedPlatform);
        }
    }

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
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            throw new InvalidOperationException(Resources.UnSupportedPlatform);
        }

        if (!Directory.Exists(AzuriteLocalStorageFolder))
        {
            Directory.CreateDirectory(AzuriteLocalStorageFolder);
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = CommandLine,
            Arguments = CommandLineArguments,
            RedirectStandardError = true
        });
        if (process.NotExists())
        {
            throw new InvalidOperationException(
                Resources.AzuriteStorageEmulator_StartEmulator_FailedStartup.Format(CommandLine, CommandLineArguments));
        }

        if (process.HasExited)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(Resources.AzuriteStorageEmulator_StartEmulator_Exited.Format(error));
        }
    }

    private static void ShutdownEmulator()
    {
        KillEmulatorProcesses();
    }

    private static IEnumerable<Process> GetRunningProcesses()
    {
        return Process.GetProcesses()
            .Where(process => process.ProcessName.EqualsIgnoreCase(NodeJsProcessName)
                              && process.StartInfo.Arguments.Contains(AzuriteJsName))
            .ToArray();
    }

    private static void KillEmulatorProcesses()
    {
        var processes = GetRunningProcesses().ToList();
        foreach (var process in processes)
        {
            Try.Safely(() => process.Kill());
        }
    }
}